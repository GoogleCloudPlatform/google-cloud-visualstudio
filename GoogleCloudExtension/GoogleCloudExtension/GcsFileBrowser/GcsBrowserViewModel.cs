// Copyright 2016 Google Inc. All Rights Reserved.
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

            IList<GcsFileOperation> uploadOperations = CreateUploadOperations(files).ToList();

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            foreach (var operation in uploadOperations)
            {
                _dataSource.StartFileUploadOperation(
                    sourcePath: operation.Source,
                    bucket: operation.Bucket,
                    name: operation.Destination,
                    operation: operation,
                    token: tokenSource.Token);
            }

            GcsFileProgressDialogWindow.PromptUser(
                caption: Resources.GcsFileBrowserUploadingProgressCaption,
                message: Resources.GcsFileBrowserUploadingProgressMessage,
                operations: uploadOperations,
                tokenSource: tokenSource);

            UpdateCurrentState();
        }

        /// <summary>
        /// Method called whenever the selection changes to udpate the view model.
        /// </summary>
        public void InvalidateSelectedItems(IEnumerable<GcsRow> selectedRows)
        {
            SelectedItems = selectedRows.ToList();

            UpdateContextMenu();
        }

        /// <summary>
        /// This method creates the <seealso cref="GcsFileOperation"/> instances that represent the upload
        /// operations for the given paths of files. If the <paramref name="sources"/> entry represents a directory
        /// then it will recurse into the directories to create the upload operations for those files as well.
        /// </summary>
        /// <param name="sources">
        /// The path to the sources to upload. These can be either directories or files. It is better if these
        /// are full file paths as the current directory in VS changes quite often.
        /// </param>
        /// <returns>The list of <seealso cref="GcsFileOperation"/> that represent the upload of the files.</returns>
        private IEnumerable<GcsFileOperation> CreateUploadOperations(IEnumerable<string> sources)
            => sources
               .Select(src =>
               {
                   var info = new FileInfo(src);
                   var isDirectory = (info.Attributes & FileAttributes.Directory) != 0;

                   if (isDirectory)
                   {
                       return CreateUploadOperationsForDirectory(info.FullName, info.Name);
                   }
                   else
                   {
                       return new GcsFileOperation[]
                          {
                            new GcsFileOperation(
                                source: info.FullName,
                                bucket: Bucket.Name,
                                destination: $"{CurrentState.CurrentPath}{info.Name}"),
                          };
                   }
               })
               .SelectMany(x => x);

        /// <summary>
        /// Creates the <seealso cref="GcsFileOperation"/> instances for all of the files in the given directory. The
        /// target directory will be based on <paramref name="basePath"/>.
        /// </summary>
        private IEnumerable<GcsFileOperation> CreateUploadOperationsForDirectory(string dir, string basePath)
            => Enumerable.Concat(
                Directory.EnumerateFiles(dir)
                    .Select(file => new GcsFileOperation(
                        source: file,
                        bucket: Bucket.Name,
                        destination: $"{_currentState.CurrentPath}{basePath}/{Path.GetFileName(file)}")),
                Directory.EnumerateDirectories(dir)
                    .Select(subDir => CreateUploadOperationsForDirectory(subDir, $"{basePath}/{Path.GetFileName(subDir)}"))
                    .SelectMany(x => x));

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
                UpdateCurrentState(row.Name);
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
            // 1) The user is prompted for the download root where to store the downloaded files.
            FBD dialog = new FBD();
            dialog.Description = Resources.GcsFileBrowserFolderSelectionMessage;
            dialog.ShowNewFolderButton = true;

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            var downloadRoot = dialog.SelectedPath;

            // 2) The files to be downloaded and directories to be created are collected.
            var downloadOperations = new List<GcsFileOperation>();
            try
            {
                IsLoading = true;

                downloadOperations.AddRange(SelectedItems
                    .Where(x => x.IsFile)
                    .Select(x => new GcsFileOperation(
                        source: x.Name,
                        bucket: Bucket.Name,
                        destination: Path.Combine(downloadRoot, x.FileName))));

                var subDirs = new HashSet<string>();
                foreach (var dir in SelectedItems.Where(x => x.IsDirectory))
                {
                    var files = await _dataSource.GetGcsFilesFromPrefixAsync(Bucket.Name, dir.Name);
                    foreach (var file in files)
                    {
                        var relativeFilePath = Path.Combine(dir.FileName, file.RelativeName.Replace('/', '\\'));
                        var absoluteFilePath = Path.Combine(downloadRoot, relativeFilePath);

                        // Create the file operation for this file.
                        downloadOperations.Add(new GcsFileOperation(
                            source: file.Name,
                            bucket: Bucket.Name,
                            destination: absoluteFilePath));

                        // Collects the list of directories to create.
                        subDirs.Add(Path.GetDirectoryName(absoluteFilePath));
                    }
                }

                // 3) Create all of the subdirectories.
                foreach (var dir in subDirs)
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (IOException)
                    {
                        UserPromptUtils.ErrorPrompt(
                            message: String.Format(Resources.GcsFileBrowserFailedToCreateDirMessage, dir),
                            title: Resources.UiErrorCaption);
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }

            // 4) Start the download operations and open the progress dialog to show the progress to the user.
            var tokenSource = new CancellationTokenSource();
            foreach (var operation in downloadOperations)
            {
                _dataSource.StartFileDownloadOperation(
                    bucket: Bucket.Name,
                    name: operation.Source,
                    destPath: operation.Destination,
                    operation: operation,
                    token: tokenSource.Token);
            }
            GcsFileProgressDialogWindow.PromptUser(
                caption: Resources.GcsFileBrowserDownloadingProgressCaption,
                message: Resources.GcsFileBrowserDownloadingProgressMessage,
                operations: downloadOperations,
                tokenSource: tokenSource);
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
            // 1) The user is asked to confirm the deletion operation.
            if (!UserPromptUtils.ActionPrompt(
                prompt: Resources.GcsFileBrowserDeletePromptMessage,
                title: Resources.UiDeleteButtonCaption,
                actionCaption: Resources.UiDeleteButtonCaption,
                cancelCaption: Resources.UiCancelButtonCaption))
            {
                return;
            }

            var deleteOperations = new List<GcsFileOperation>();
            try
            {
                IsLoading = true;

                // 2) Collect all of the files to be deleted and create the delete operations to track
                //    the delete progress.
                deleteOperations.AddRange(SelectedItems
                   .Where(x => x.IsFile)
                   .Select(x => new GcsFileOperation(
                       source: x.Name,
                       bucket: Bucket.Name,
                       destination: null)));

                var filesInSubdirectories = await _dataSource.GetGcsFilesFromPrefixesAsync(
                    Bucket.Name,
                    SelectedItems.Where(x => x.IsDirectory).Select(x => x.Name));
                deleteOperations.AddRange(filesInSubdirectories.Select(x => new GcsFileOperation(
                    source: x.Name,
                    bucket: Bucket.Name)));
            }
            catch (DataSourceException)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.GcsFileBrowserDeleteListErrorMessage,
                    title: Resources.UiErrorCaption);
            }
            finally
            {
                IsLoading = false;
            }

            // 3) start the deletion operations and open the progress dialog.
            var tokenSource = new CancellationTokenSource();
            foreach (var operation in deleteOperations)
            {
                _dataSource.StartDeleteOperation(
                    bucket: Bucket.Name,
                    name: operation.Source,
                    operation: operation,
                    token: tokenSource.Token);
            }
            GcsFileProgressDialogWindow.PromptUser(
                caption: Resources.GcsFileBrowserDeletingProgressCaption,
                message: Resources.GcsFileBrowserDeletingProgressMessage,
                operations: deleteOperations,
                tokenSource: tokenSource);

            // 4) refresh the window with the contents of the server.
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
