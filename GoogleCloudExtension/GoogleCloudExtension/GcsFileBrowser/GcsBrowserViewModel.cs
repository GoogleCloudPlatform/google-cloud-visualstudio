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
    public class GcsBrowserViewModel : ViewModelBase
    {
        private readonly static GcsBrowserState s_emptyState =
            new GcsBrowserState(Enumerable.Empty<GcsRow>(), "/");

        private readonly GcsFileBrowserWindow _owner;
        private readonly SelectionUtils _selectionUtils;
        private Bucket _bucket;
        private GcsDataSource _dataSource;
        private bool _isLoading;
        private readonly List<GcsBrowserState> _stateStack = new List<GcsBrowserState>();
        private IList<GcsRow> _selectedItems;
        private ContextMenu _contextMenu;

        public Bucket Bucket
        {
            get { return _bucket; }
            set
            {
                SetValueAndRaise(ref _bucket, value);
                InvalidateBucket();
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                SetValueAndRaise(ref _isLoading, value);
                RaisePropertyChanged(nameof(IsReady));
            }
        }

        public GcsBrowserState Top
        {
            get
            {
                if (_stateStack.Count == 0)
                {
                    return s_emptyState;
                }
                return _stateStack.Last();
            }
        }

        public bool IsReady => !IsLoading;

        public IList<GcsRow> SelectedItems
        {
            get { return _selectedItems; }
            private set
            {
                SetValueAndRaise(ref _selectedItems, value);
                InvalidateSelectedItem();
            }
        }

        public ContextMenu ContextMenu
        {
            get { return _contextMenu; }
            private set { SetValueAndRaise(ref _contextMenu, value); }
        }

        public GcsRow SelectedItem => SelectedItems.FirstOrDefault();

        public ICommand PopAllCommand { get; }

        public ICommand NavigateToCommand { get; }

        public ICommand ShowDirectoryCommand { get; }

        public ICommand RefreshCommand { get; }

        public GcsBrowserViewModel(GcsFileBrowserWindow owner)
        {
            _owner = owner;
            _selectionUtils = new SelectionUtils(owner);

            PopAllCommand = new ProtectedCommand(OnPopAllCommand);
            NavigateToCommand = new ProtectedCommand<string>(OnNavigateToCommand);
            ShowDirectoryCommand = new ProtectedCommand<GcsRow>(OnShowDirectoryCommand);
            RefreshCommand = new ProtectedCommand(OnRefreshCommand);
        }

        public async void StartFileUpload(string[] files)
        {
            var uploadOperations = CreateUploadOperations(files);

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            foreach (var operation in uploadOperations)
            {
                _dataSource.StartUploadOperation(
                    sourcePath: operation.Source,
                    bucket: operation.Bucket,
                    name: operation.Destination,
                    operation: operation,
                    token: tokenSource.Token);
            }

            GcsFileProgressDialogWindow.PromptUser(
                caption: "Upload Files",
                message: "Files being uploaded",
                operations: uploadOperations,
                tokenSource: tokenSource);

            RefreshTopState();
        }

        private List<GcsFileOperation> CreateUploadOperations(string[] files)
        {
            var result = new List<GcsFileOperation>();

            foreach (var input in files)
            {
                var info = new FileInfo(input);
                var isDirectory = (info.Attributes & FileAttributes.Directory) != 0;

                if (isDirectory)
                {
                    result.AddRange(CreateUploadOperationsForDirectory(info.FullName, info.Name));
                }
                else
                {
                    result.Add(new GcsFileOperation(
                        source: info.FullName,
                        bucket: Bucket.Name,
                        destination: $"{Top.CurrentPath}{info.Name}"));
                }
            }

            return result;
        }

        private IEnumerable<GcsFileOperation> CreateUploadOperationsForDirectory(string dir, string localPath)
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                yield return new GcsFileOperation(
                    source: file,
                    bucket: Bucket.Name,
                    destination: $"{Top.CurrentPath}{localPath}/{Path.GetFileName(file)}");
            }

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                foreach (var operation in CreateUploadOperationsForDirectory(subDir, $"{localPath}/{Path.GetFileName(subDir)}"))
                {
                    yield return operation;
                }
            }
        }

        public void InvalidateSelectedItems(IEnumerable<GcsRow> selectedRows)
        {
            SelectedItems = selectedRows.ToList();

            UpdateContextMenu();
        }

        private void UpdateContextMenu()
        {
            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = "New Folder...", Command=new ProtectedCommand(OnNewFolderCommand) },
                new MenuItem { Header = "Download...", Command=new ProtectedCommand(OnDownloadCommand, canExecuteCommand: CanDownloadItems()) },
                new MenuItem { Header = "Delete", Command = new ProtectedCommand(OnDeleteCommand, canExecuteCommand: SelectedItems != null && SelectedItems.Count > 0) },
            };

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private bool CanDownloadItems()
        {
            return SelectedItems != null && SelectedItems.Where(x => x.IsFile).Count() > 0;
        }

        private void OnDownloadCommand()
        {
            FBD dialog = new FBD();
            dialog.Description = "Download files";
            dialog.ShowNewFolderButton = true;

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            var downloadOperations = SelectedItems
                .Where(x => x.IsFile)
                .Select(x => new GcsFileOperation(
                    source: x.Name,
                    bucket: Bucket.Name,
                    destination: Path.Combine(dialog.SelectedPath, x.FileName)))
                .ToList();

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            foreach (var operation in downloadOperations)
            {
                _dataSource.StartDownloadOperation(
                    bucket: Bucket.Name,
                    name: operation.Source,
                    destPath: operation.Destination,
                    operation: operation,
                    token: tokenSource.Token);
            }

            GcsFileProgressDialogWindow.PromptUser(
                caption: "Download Files",
                message: "Files being downloaded",
                operations: downloadOperations,
                tokenSource: tokenSource);
        }

        private async void OnDeleteCommand()
        {
            if (!UserPromptUtils.ActionPrompt(
                prompt: "Are you sure you want to delete these files?",
                title: "Delete",
                actionCaption: Resources.UiDeleteButtonCaption,
                cancelCaption: Resources.UiCancelButtonCaption))
            {
                return;
            }


            var deleteOperations = new List<GcsFileOperation>();
            var tokenSource = new CancellationTokenSource();

            try
            {
                IsLoading = true;

                deleteOperations.AddRange(SelectedItems
                   .Where(x => x.IsFile)
                   .Select(x => new GcsFileOperation(
                       source: x.Name,
                       bucket: Bucket.Name,
                       destination: null)));
                deleteOperations.AddRange(await GetSubDirectoryFiles(SelectedItems.Where(x => x.IsDirectory)));

                foreach (var operation in deleteOperations)
                {
                    _dataSource.StartDeleteOperation(
                        bucket: Bucket.Name,
                        name: operation.Source,
                        operation: operation,
                        token: tokenSource.Token);
                }
            }
            catch (DataSourceException ex)
            {

            }
            finally
            {
                IsLoading = false;
            }

            GcsFileProgressDialogWindow.PromptUser(
                caption: "Deleting Files",
                message: "Files being deleted",
                operations: deleteOperations,
                tokenSource: tokenSource);

            InvalidateStack();
            RefreshTopState();
        }

        private async Task<IEnumerable<GcsFileOperation>> GetSubDirectoryFiles(IEnumerable<GcsRow> rows)
        {
            var result = new List<GcsFileOperation>();

            foreach (var row in rows)
            {
                var files = await _dataSource.GetObjectLisAsync(Bucket.Name, row.Name);
                result.AddRange(files.Select(x => new GcsFileOperation(
                    source: x.Name,
                    bucket: Bucket.Name,
                    destination: null)));
            }

            return result;
        }

        #region Command handlers

        private void OnNewFolderCommand()
        {
            throw new NotImplementedException();
        }

        private void OnPopAllCommand()
        {
            PopToRoot();
        }

        private void OnNavigateToCommand(string step)
        {
            PopToState(step);
        }

        private void OnShowDirectoryCommand(GcsRow dir)
        {
            PushToDirectory(dir.Name);
        }

        private void OnRefreshCommand()
        {
            RefreshTopState();
        }

        #endregion

        #region Navigation stack methods

        private void InvalidateStack()
        {
            foreach (var entry in _stateStack)
            {
                entry.NeedsRefresh = true;
            }
        }

        private void InvalidateTop()
        {
            if (Top.NeedsRefresh)
            {
                RefreshTopState();
            }
            else
            {
                RaisePropertyChanged(nameof(Top));
            }
        }

        private async void RefreshTopState()
        {
            GcsBrowserState newState;
            try
            {
                IsLoading = true;

                newState = await LoadStateForDirectoryAsync(Top.CurrentPath);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to refersh directory {Top.Name}: {ex.Message}");
                newState = CreateErrorState(Top.Name);
            }
            finally
            {
                IsLoading = false;
            }

            _stateStack[_stateStack.Count - 1] = newState;
            RaisePropertyChanged(nameof(Top));
        }


        private void PopToRoot()
        {
            _stateStack.RemoveRange(1, _stateStack.Count - 1);

            InvalidateTop();
        }

        private void PopState()
        {
            if (_stateStack.Count == 1)
            {
                return;
            }

            _stateStack.RemoveRange(_stateStack.Count - 1, 1);
            InvalidateTop();
        }

        private void PopToState(string step)
        {
            var idx = _stateStack.FindIndex(x => x.Name == step);
            if (idx == -1)
            {
                Debug.WriteLine($"Could not find {step}");
            }

            _stateStack.RemoveRange(idx + 1, _stateStack.Count - (idx + 1));
            InvalidateTop();
        }

        private async void PushToDirectory(string name)
        {
            GcsBrowserState state;
            try
            {
                state = await LoadStateForDirectoryAsync(name);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to load directory {name}: {ex.Message}");
                state = CreateErrorState(name);
            }

            _stateStack.Add(state);
            InvalidateTop();
        }

        #endregion

        private void InvalidateBucket()
        {
            _dataSource = new GcsDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            PushToDirectory("");
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
                var items = Enumerable.Concat<GcsRow>(
                    dir.Prefixes.OrderBy(x => x).Select(GcsRow.CreateDirectoryRow),
                    dir.Items.OrderBy(x => x.Name).Select(GcsRow.CreateFileRow));

                return new GcsBrowserState(items, name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static GcsBrowserState CreateErrorState(string name) =>
            new GcsBrowserState(new List<GcsRow> { GcsRow.CreateErrorRow($"Failed to load directory {name}.") }, name);
    }
}
