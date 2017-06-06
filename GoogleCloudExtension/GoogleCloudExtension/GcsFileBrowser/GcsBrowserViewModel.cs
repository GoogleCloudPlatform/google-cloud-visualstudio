// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GcsFileProgressDialog;
using GoogleCloudExtension.GcsUtils;
using GoogleCloudExtension.NamePrompt;
using GoogleCloudExtension.ProgressDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using FBD = System.Windows.Forms.FolderBrowserDialog;

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// The view model for the file brwoser.
    /// </summary>
    public class GcsBrowserViewModel : ViewModelBase
    {
        private readonly static GcsBrowserState s_emptyState =
            new GcsBrowserState(Enumerable.Empty<GcsRow>(), "");

        private readonly GcsFileBrowserWindow _owner;
        private readonly SelectionUtils _selectionUtils;
        private Bucket _bucket;
        private GcsDataSource _dataSource;
        private FileOperationsEngine _fileOperationsEngine;
        private bool _isLoading;
        private IList<GcsRow> _selectedItems;
        private GcsBrowserState _currentState;

        /// <summary>
        /// The current navigation state for the browser.
        /// </summary>
        public GcsBrowserState CurrentState
        {
            get { return _currentState; }
            private set { SetValueAndRaise(ref _currentState, value); }
        }

        /// <summary>
        /// The bucket that is being shown in the window.
        /// </summary>
        public Bucket Bucket
        {
            get { return _bucket; }
            internal set
            {
                SetValueAndRaise(ref _bucket, value);
                InvalidateBucket();
            }
        }

        /// <summary>
        /// Whether the window is busy loading data.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                SetValueAndRaise(ref _isLoading, value);
                RaisePropertyChanged(nameof(IsReady));
            }
        }

        /// <summary>
        /// Utility property used to determine if the window is ready for user input.
        /// </summary>
        public bool IsReady => !IsLoading;

        /// <summary>
        /// The list of selected items.
        /// </summary>
        public IList<GcsRow> SelectedItems
        {
            get { return _selectedItems; }
            private set
            {
                SetValueAndRaise(ref _selectedItems, value);
                InvalidateSelectedItem();
            }
        }

        /// <summary>
        /// The first selected item.
        /// </summary>
        public GcsRow SelectedItem => SelectedItems?.FirstOrDefault();

        /// <summary>
        /// Command to execute to navigate to the root of the bucket.
        /// </summary>
        public ICommand NavigateToRootCommand { get; }

        /// <summary>
        /// Command to execute when navigating to a step in the path into the bucket.
        /// </summary>
        public ICommand NavigateToCommand { get; }

        /// <summary>
        /// Command to execute when refreshing the currently loaded path.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// The command to execute when a double click happens.
        /// </summary>
        public ICommand DoubleClickCommand { get; }

        public GcsBrowserViewModel(GcsFileBrowserWindow owner)
        {
            _owner = owner;
            _selectionUtils = new SelectionUtils(owner);
            _currentState = s_emptyState;

            NavigateToRootCommand = new ProtectedCommand(OnNavigateToRootCommand);
            NavigateToCommand = new ProtectedCommand<PathStep>(OnNavigateToCommand);
            RefreshCommand = new ProtectedCommand(OnRefreshCommand);
            DoubleClickCommand = new ProtectedCommand<GcsRow>(OnDoubleClickCommand);
        }

        /// <summary>
        /// Method called to upload the given set of files.
        /// </summary>
        /// <param name="files">The list of files to upload to the bucket.</param>
        public void StartDroppedFilesUpload(IEnumerable<string> files)
        {
            // Attempts to set VS as the foreground window so the user can see the progress
            // of the upload operation. Similar to what is done in the Windows explorer.
            ShellUtils.SetForegroundWindow();

            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                var uploadOperationsQueue = _fileOperationsEngine.StartUploadOperations(
                    files,
                    bucket: Bucket.Name,
                    bucketPath: _currentState.CurrentPath,
                    cancellationToken: cancellationTokenSource.Token);

                GcsFileProgressDialogWindow.PromptUser(
                    caption: Resources.GcsFileBrowserUploadingProgressCaption,
                    message: Resources.GcsFileBrowserUploadingProgressMessage,
                    progressMessage: Resources.GcsFileBrowserUploadingOverallProgressMessage,
                    operations: uploadOperationsQueue.Operations,
                    cancellationTokenSource: cancellationTokenSource);
            }
            catch (IOException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.GcsFileProgressDialogFailedToEnumerateFiles,
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
            }

            UpdateCurrentState();
        }

        /// <summary>
        /// Method called whenever the selection changes to update the view model.
        /// </summary>
        internal void InvalidateSelectedItems(IEnumerable<GcsRow> selectedRows)
        {
            SelectedItems = selectedRows.ToList();
        }

        internal ContextMenu GetGridContextMenu()
        {
            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.GcsFileBrowserNewFolderHeader, Command = new ProtectedCommand(OnNewFolderCommand) },
                new MenuItem { Header = Resources.UiSelectAllHeader, Command = new ProtectedCommand(OnSelectAllCommand, canExecuteCommand: !CurrentState.IsEmpty) }
            };

            return new ContextMenu { ItemsSource = menuItems };
        }

        internal ContextMenu GetItemsContextMenu()
        {
            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.GcsFileBrowserDonwloadHeader, Command=new ProtectedCommand(OnDownloadCommand) },
                new MenuItem { Header = Resources.UiDeleteButtonCaption, Command = new ProtectedCommand(OnDeleteCommand) },
            };

            if (SelectedItems.Count == 1)
            {
                if (SelectedItem.IsFile)
                {
                    menuItems.Add(new MenuItem
                    {
                        Header = Resources.GcsFileBrowserRenameFileHeader,
                        Command = new ProtectedCommand(OnRenameFileCommand)
                    });
                }
                else if (SelectedItem.IsDirectory)
                {
                    menuItems.Add(new MenuItem
                    {
                        Header = Resources.GcsFileBrowserRenameDirectoryHeader,
                        Command = new ProtectedCommand(OnRenameDirectoryCommand)
                    });
                }
            }

            return new ContextMenu { ItemsSource = menuItems };
        }

        #region Command handlers

        private void OnSelectAllCommand()
        {
            _owner.SelectAllRows();
        }

        private async void OnRenameDirectoryCommand()
        {
            var newLeafName = NamePromptWindow.PromptUser(SelectedItem.LeafName);
            if (newLeafName == null)
            {
                return;
            }

            try
            {
                IsLoading = true;

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                var renameDirectoryOperations = await _fileOperationsEngine.StartDirectoryRenameOperationsAsync(
                    bucket: Bucket.Name,
                    parentName: CurrentState.CurrentPath,
                    oldLeafName: SelectedItem.LeafName,
                    newLeafName: newLeafName,
                    cancellationToken: cancellationTokenSource.Token);

                GcsFileProgressDialogWindow.PromptUser(
                    caption: Resources.GcsFileBrowserRenamingFilesCaption,
                    message: Resources.GcsFileBrowserRenamingFilesMessage,
                    progressMessage: Resources.GcsFileBrowserRenamingFilesProgressMessage,
                    operations: renameDirectoryOperations.Operations,
                    cancellationTokenSource: cancellationTokenSource);
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: string.Format(Resources.GcsFileBrowserRenameFailedMessage, SelectedItem.LeafName),
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
            }
            finally
            {
                IsLoading = false;
            }

            UpdateCurrentState();
        }

        private async void OnRenameFileCommand()
        {
            var choosenName = NamePromptWindow.PromptUser(SelectedItem.LeafName);
            if (choosenName == null)
            {
                return;
            }

            try
            {
                IsLoading = true;

                var newName = GcsPathUtils.Combine(CurrentState.CurrentPath, choosenName);
                Debug.WriteLine($"Renaming {SelectedItem.BlobName} to {newName}");
                await ProgressDialogWindow.PromptUser(
                    _dataSource.MoveFileAsync(
                        bucket: Bucket.Name,
                        sourceName: SelectedItem.BlobName,
                        destName: newName),
                    new ProgressDialogWindow.Options
                    {
                        Message = Resources.GcsFileBrowserRenamingProgressMessage,
                        Title = Resources.UiDefaultPromptTitle,
                        IsCancellable = false
                    });
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: string.Format(Resources.GcsFileBrowserRenameFailedMessage, SelectedItem.LeafName),
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
            }
            finally
            {
                IsLoading = false;
            }

            UpdateCurrentState();
        }

        private void OnDoubleClickCommand(GcsRow row)
        {
            if (row.IsDirectory)
            {
                UpdateCurrentState(row.BlobName);
            }
            else
            {
                // TODO: Show the file.
            }
        }

        private async void OnDownloadCommand()
        {
            FBD dialog = new FBD();
            dialog.Description = Resources.GcsFileBrowserFolderSelectionMessage;
            dialog.ShowNewFolderButton = true;

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            var downloadRoot = dialog.SelectedPath;

            try
            {
                IsLoading = true;

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                var downloadOperationsQueue = await _fileOperationsEngine.StartDownloadOperationsAsync(
                    SelectedItems.Select(x => new GcsUtils.GcsItemRef(x.Bucket, x.BlobName)),
                    downloadRoot,
                    cancellationTokenSource.Token);

                GcsFileProgressDialogWindow.PromptUser(
                    caption: Resources.GcsFileBrowserDownloadingProgressCaption,
                    message: Resources.GcsFileBrowserDownloadingProgressMessage,
                    progressMessage: Resources.GcsFileBrowserDownloadingOverallProgressMessage,
                    operations: downloadOperationsQueue.Operations,
                    cancellationTokenSource: cancellationTokenSource);
            }
            catch (IOException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.GcsFileBrowserFailedToCreateDirMessage,
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnDeleteCommand()
        {
            if (!UserPromptUtils.ActionPrompt(
                prompt: Resources.GcsFileBrowserDeletePromptMessage,
                title: Resources.UiDefaultPromptTitle,
                actionCaption: Resources.UiDeleteButtonCaption,
                cancelCaption: Resources.UiCancelButtonCaption))
            {
                return;
            }

            try
            {
                IsLoading = true;


                var cancellationTokenSource = new CancellationTokenSource();
                var deleteOperationsQueue = await _fileOperationsEngine.StartDeleteOperationsAsync(
                    SelectedItems.Select(x => new GcsItemRef(x.Bucket, x.BlobName)),
                    cancellationTokenSource.Token);

                GcsFileProgressDialogWindow.PromptUser(
                    caption: Resources.GcsFileBrowserDeletingProgressCaption,
                    message: Resources.GcsFileBrowserDeletingProgressMessage,
                    progressMessage: Resources.GcsFileBrowserDeletingOverallProgressMessage,
                    operations: deleteOperationsQueue.Operations,
                    cancellationTokenSource: cancellationTokenSource);
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.GcsFileBrowserDeleteListErrorMessage,
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
            }
            finally
            {
                IsLoading = false;
            }

            UpdateCurrentState();
        }

        private async void OnNewFolderCommand()
        {
            var name = NamePromptWindow.PromptUser();
            if (name == null)
            {
                return;
            }

            try
            {
                IsLoading = true;

                await _dataSource.CreateDirectoryAsync(Bucket.Name, $"{CurrentState.CurrentPath}{name}/");
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: String.Format(Resources.GcsFileBrowserFailedToCreateRemoteFolder, name),
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
            }
            finally
            {
                IsLoading = false;
            }

            UpdateCurrentState();
        }

        private void OnNavigateToRootCommand()
        {
            UpdateCurrentState("");
        }

        private void OnNavigateToCommand(PathStep step)
        {
            UpdateCurrentState(step.Path);
        }

        private void OnRefreshCommand()
        {
            UpdateCurrentState();
        }

        #endregion

        private void UpdateCurrentState()
        {
            UpdateCurrentState(CurrentState.CurrentPath);
        }

        private async void UpdateCurrentState(string newPath)
        {
            GcsBrowserState newState;
            try
            {
                // Reset the error and empty state while loading.
                IsLoading = true;
                newState = await LoadStateForDirectoryAsync(newPath);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to update to path {newPath}: {ex.Message}");
                newState = CreateErrorState(newPath);
            }
            finally
            {
                IsLoading = false;
            }

            CurrentState = newState;
        }

        private void InvalidateBucket()
        {
            _dataSource = new GcsDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            _fileOperationsEngine = new FileOperationsEngine(_dataSource);
            UpdateCurrentState("");
        }

        private void InvalidateSelectedItem()
        {
            RaisePropertyChanged(nameof(SelectedItem));

            if (SelectedItem != null)
            {
                PropertyWindowItemBase item;
                if (SelectedItem.IsDirectory)
                {
                    item = new GcsDirectoryItem(SelectedItem);
                }
                else
                {
                    item = new GcsFileItem(SelectedItem);
                }
                _selectionUtils.SelectItem(item);
            }
            else
            {
                _selectionUtils.ClearSelection();
            }
        }

        private async Task<GcsBrowserState> LoadStateForDirectoryAsync(string name)
        {
            try
            {
                IsLoading = true;

                var dir = await _dataSource.GetDirectoryListAsync(Bucket.Name, name);
                var items = Enumerable.Concat(
                    dir.Directories.OrderBy(d => d).Select(d => GcsRow.CreateDirectoryRow(bucket: Bucket.Name, name: d)),
                    dir.Files.Where(f => f.Name.Last() != '/').OrderBy(f => f.Name).Select(GcsRow.CreateFileRow));

                return new GcsBrowserState(items, name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static GcsBrowserState CreateErrorState(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new GcsBrowserState(Resources.GcsFileBrowserFailedLoadRootDirectoryMessage, name);
            }
            else
            {
                return new GcsBrowserState(String.Format(Resources.GcsFileBrowserFailedDirectoryLoadMessage, name), name);
            }
        }
    }
}
