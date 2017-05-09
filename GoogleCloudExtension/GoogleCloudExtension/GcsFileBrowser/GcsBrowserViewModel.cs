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
        private ContextMenu _contextMenu;
        private GcsBrowserState _currentState;

        /// <summary>
        /// The current navigation state for the browser.
        /// </summary>
        public GcsBrowserState CurrentState
        {
            get { return _currentState; }
            set { SetValueAndRaise(ref _currentState, value); }
        }

        /// <summary>
        /// The bucket that is being shown in the window.
        /// </summary>
        public Bucket Bucket
        {
            get { return _bucket; }
            set
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
        /// The context menu to show.
        /// </summary>
        public ContextMenu ContextMenu
        {
            get { return _contextMenu; }
            private set { SetValueAndRaise(ref _contextMenu, value); }
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

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var uploadOperations = _fileOperationsEngine.StartUploadOperations(
                files,
                bucket: Bucket.Name,
                bucketPath: _currentState.CurrentPath,
                cancellationToken: cancellationTokenSource.Token);

            GcsFileProgressDialogWindow.PromptUser(
                caption: Resources.GcsFileBrowserUploadingProgressCaption,
                message: Resources.GcsFileBrowserUploadingProgressMessage,
                operations: uploadOperations,
                cancellationTokenSource: cancellationTokenSource);

            UpdateCurrentState();
        }

        /// <summary>
        /// Method called whenever the selection changes to update the view model.
        /// </summary>
        public void InvalidateSelectedItems(IEnumerable<GcsRow> selectedRows)
        {
            SelectedItems = selectedRows.ToList();

            UpdateContextMenu();
        }

        private void UpdateContextMenu()
        {
            var hasItems = SelectedItems != null && SelectedItems.Count > 0;
            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.GcsFileBrowserNewFolderHeader, Command=new ProtectedCommand(OnNewFolderCommand) },
                new MenuItem { Header = Resources.GcsFileBrowserDonwloadHeader, Command=new ProtectedCommand(OnDownloadCommand, canExecuteCommand: hasItems) },
                new MenuItem { Header = Resources.UiDeleteButtonCaption, Command = new ProtectedCommand(OnDeleteCommand, canExecuteCommand: hasItems) },
            };

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        #region Command handlers

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

        /// <summary>
        /// The download command is implemented with the following steps:
        /// 1) The user is prompted for the directory that will serve as the root of all of the downloads.
        /// 2) The list of files to be downloaded, including subdirectories is collected.
        /// 3) The subdirectories that will receive downloads are created under the download root.
        /// 4) The file operations are started and the progress dialog is shown to the user. The same <seealso cref="CancellationToken"/>
        ///    is used for all of the operations so the progress dialog can be used to cancel all operations.
        /// </summary>
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

            IList<GcsFileOperation> downloadOperations;
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {
                IsLoading = true;

                downloadOperations = await _fileOperationsEngine.StartDownloadOperationsAsync(
                    SelectedItems.Select(x => new GcsUtils.GcsItemRef(x.Bucket, x.BlobName)),
                    downloadRoot,
                    cancellationTokenSource.Token);
            }
            catch (IOException)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.GcsFileBrowserFailedToCreateDirMessage,
                    title: Resources.UiErrorCaption);
                return;
            }
            finally
            {
                IsLoading = false;
            }

            GcsFileProgressDialogWindow.PromptUser(
                caption: Resources.GcsFileBrowserDownloadingProgressCaption,
                message: Resources.GcsFileBrowserDownloadingProgressMessage,
                operations: downloadOperations,
                cancellationTokenSource: cancellationTokenSource);
        }

        /// <summary>
        /// The delete command is implemented with the following steps:
        /// 1) The user is asked to confirm the operation.
        /// 2) The files to be deleted are collected, this involves listing requests sent to the server for
        ///    the subdirectories being deleted. For each file to be deleted a <seealso cref="GcsFileOperation"/> instance
        ///    is created to track the progress of the operation. Each delete operation is independent.
        /// 3) The operations are started and the progress dialog that tracks the progress of all of the operations is opened. All of
        ///    operations use the same <seealso cref="CancellationToken"/> and the <seealso cref="CancellationTokenSource"/> is
        ///    passed to the progress dialog so the user can cancel the operations.
        /// 4) The listing of objects is invalidated and the window refreshed.  
        /// </summary>
        private async void OnDeleteCommand()
        {
            if (!UserPromptUtils.ActionPrompt(
                prompt: Resources.GcsFileBrowserDeletePromptMessage,
                title: Resources.UiDeleteButtonCaption,
                actionCaption: Resources.UiDeleteButtonCaption,
                cancelCaption: Resources.UiCancelButtonCaption))
            {
                return;
            }

            IList<GcsFileOperation> deleteOperations;
            var cancellationTokenSource = new CancellationTokenSource();
            try
            {
                IsLoading = true;

                deleteOperations = await _fileOperationsEngine.StartDeleteOperationsAsync(
                    SelectedItems.Select(x => new GcsItemRef(x.Bucket, x.BlobName)),
                    cancellationTokenSource.Token);
            }
            catch (DataSourceException)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.GcsFileBrowserDeleteListErrorMessage,
                    title: Resources.UiErrorCaption);
                return;
            }
            finally
            {
                IsLoading = false;
            }

            GcsFileProgressDialogWindow.PromptUser(
                caption: Resources.GcsFileBrowserDeletingProgressCaption,
                message: Resources.GcsFileBrowserDeletingProgressMessage,
                operations: deleteOperations,
                cancellationTokenSource: cancellationTokenSource);

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
            catch (DataSourceException)
            {
                UserPromptUtils.ErrorPrompt(
                    message: String.Format(Resources.GcsFileBrowserFailedToCreateRemoteFolder, name),
                    title: Resources.UiErrorCaption);
                return;
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

        private static GcsBrowserState CreateErrorState(string name) =>
            new GcsBrowserState(
                new List<GcsRow>
                {
                    GcsRow.CreateErrorRow(String.Format(Resources.GcsFileBrowserFailedDirectoryLoadMessage, name))
                },
                name);
    }
}
