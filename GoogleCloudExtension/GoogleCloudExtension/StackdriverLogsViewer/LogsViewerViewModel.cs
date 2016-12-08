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

using Google.Apis.Logging.v2.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.StackdriverLogsViewer
{

    /// <summary>
    /// The view model for LogsViewerToolWindow.
    /// </summary>
    public class LogsViewerViewModel : ViewModelBase
    {
        private const string CloudLogo20Path = "StackdriverLogsViewer/Resources/logo_cloud.png";

        private static readonly Lazy<ImageSource> s_cloud_logo_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(CloudLogo20Path));


        public ImageSource CloudLogo => s_cloud_logo_icon.Value;


        private string _nextPageToken;
        private string _loadingProgress;
        private Lazy<LoggingDataSource> _dataSource;
        private ProtectedCommand _cancelLoadingCommand;
        private ProtectedCommand _toggleExpandAllCommand;
        private DataGridRowDetailsVisibilityMode _expandAll = DataGridRowDetailsVisibilityMode.Collapsed;
        public string CancelButtonText => "Cancel";

        private bool _canCallNextPage = false;
        public ICommand CancelLoadingCommand => _cancelLoadingCommand;
        private Visibility _cancelLoadingVisible = Visibility.Collapsed;
        public Visibility CancelLoadingVisibility
        {
            get
            {
                return _cancelLoadingVisible;
            }

            set
            {
                SetValueAndRaise(ref _cancelLoadingVisible, value);
            }
        }

        private Visibility _messageBoradVisibility = Visibility.Collapsed;
        public Visibility ProgressErrorMessageVisibility
        {
            get
            {
                return _messageBoradVisibility;
            }

            set
            {
                SetValueAndRaise(ref _messageBoradVisibility, value);
            }
        }

        private Visibility _loadingBlockVisibility = Visibility.Collapsed;
        public Visibility LoadingBlockVisibility
        {
            get
            {
                return _loadingBlockVisibility;
            }

            set
            {
                SetValueAndRaise(ref _loadingBlockVisibility, value);
            }
        }


        public bool ToggleExapandAllExpanded { private get; set; }


        public ICommand ToggleExpandAllCommand => _toggleExpandAllCommand;

        private string _selectedDate = string.Empty;

        public void SetSelectedChanged(object item)
        {
            var log = item as LogItem;
            if (log == null)
            {
                return;
            }

            SelectedDate = log.Date;
        }

        public string SelectedDate
        {
            get
            {
                return _selectedDate;
            }
            set
            {
                if (value != _selectedDate)
                {
                    SetValueAndRaise(ref _selectedDate, value);
                }
            }
        }

        private ObservableCollection<LogItem> _logs = new ObservableCollection<LogItem>();
        ListCollectionView _collectionView;
        private Object _collectionViewLock = new Object();
        private string _filter;

        private const string ShuffleIconPath = "StackdriverLogsViewer/Resources/shuffle.png";
        private static readonly Lazy<ImageSource> s_shuffle_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ShuffleIconPath));
        public ImageSource ShuffleImage => s_shuffle_icon.Value;

        /// <summary>
        /// Append a set of log entries.
        /// </summary>
        public void AddLogs(IList<LogEntry> logEntries)
        {
            if (logEntries == null)
            {
                return;
            }

            foreach (var log in logEntries)
            {
                _logs.Add(new LogItem(log));
                // _collectionView.AddNewItem(new LogItem(log));
            }

            RaisePropertyChanged(nameof(LogEntryList));
        }

        private bool descendingOrder;

        /// <summary>
        /// Replace the current log entries with the new set.
        /// </summary>
        /// <param name="logEntries">The log entries list.</param>
        /// <param name="descending">True: Descending order by TimeStamp, False: Ascending order by TimeStamp </param>
        public void SetLogs(IList<LogEntry> logEntries, bool descending)
        {
            descendingOrder = descending;
            _logs.Clear();
            _collectionView = new ListCollectionView(new List<LogItem>());

            if (logEntries == null)
            {
                RaisePropertyChanged(nameof(LogEntryList));
                return;
            }

            AddLogs(logEntries);
        }

        public ListCollectionView LogEntryList
        {
            get
            {
                lock (_collectionViewLock)
                {
                    var sorted = descendingOrder ? _logs.OrderByDescending(x => x.Time) : _logs.OrderBy(x => x.Time);
                    var sorted_collection = new ObservableCollection<LogItem>(sorted);
                    _collectionView = new ListCollectionView(sorted_collection);
                    _collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Date"));
                    return _collectionView;
                }
            }
        }

        public string MessageFilter
        {
            get
            {
                return _filter;
            }

            set
            {
                _filter = value;
                Debug.WriteLine($"MessageFilter is called {_filter}");
                if (_collectionView == null)
                {
                    Debug.WriteLine($"set MessageFilter, _collectionView is still null");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_filter))
                {
                    _collectionView.Filter = null;
                    return;
                }

                lock (_collectionViewLock)
                {
                    var splits = _filter.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    _collectionView.Filter = new Predicate<object>(item => {
                        foreach (var subFilter in splits)
                        {
                            if (((LogItem)item).Message.Contains(subFilter))
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                    // TODO: Add filter changed event handler
                    // So that LogsViewerViewModel can disable the next page button.
                    _canCallNextPage = false;
                }
            }
        }

        /// <summary>
        /// Initializes the class.
        /// </summary>
        public LogsViewerViewModel()
        {
            _toggleExpandAllCommand = new ProtectedCommand(ToggleExpandAll, canExecuteCommand: true);
            _cancelLoadingCommand = new ProtectedCommand(() => 
            {
                Debug.WriteLine("Cancel is called");
                LogLoddingProgress = "Cancelling . . .";
                CancelLoadingVisibility = Visibility.Collapsed;
                _cancelled = true;
            });

            _refreshCommand = new ProtectedCommand(OnRefreshCommand, canExecuteCommand: false);

            _dataSource = new Lazy<LoggingDataSource>(CreateDataSource);            
        }

        public async void LoadOnStartup()
        {
            RaiseAllPropertyChanged();

            if (string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentAccount?.AccountName) || 
                string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentProjectId))
            {
                return;
            }

            await Reload();
        }

        public DataGridRowDetailsVisibilityMode ToggleExpandHideAll
        {
            get
            {
                return _expandAll;
            }
            set
            {
                SetValueAndRaise(ref _expandAll, value);    
            }
        } 

        public string Account
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentAccount?.AccountName))
                {
                    return null;
                }
                else
                {
                    return CredentialsStore.Default?.CurrentAccount?.AccountName;
                }
            }
        }

        public string Project
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentProjectId))
                {
                    return Account == null ? null : "Go to Google Cloud Explore to choose an account";
                }
                else
                {
                    return CredentialsStore.Default.CurrentProjectId;
                }
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }

            private set
            {
                SetValueAndRaise(ref _errorMessage, value);
                if (string.IsNullOrWhiteSpace(value))
                {
                    ProgressErrorMessageVisibility = Visibility.Collapsed;
                }
                else
                {
                    ProgressErrorMessageVisibility = Visibility.Visible;
                }
            }
        }

        public string LogLoddingProgress
        {
            get
            {
                return _loadingProgress;
            }

            private set
            {
                SetValueAndRaise(ref _loadingProgress, value);
                if (string.IsNullOrWhiteSpace(value))
                {
                    LoadingBlockVisibility = Visibility.Collapsed;
                }
                else
                {
                    LoadingBlockVisibility = Visibility.Visible;
                }
            }
        }


        private async Task<bool> ShouldKeepResourceType(MonitoredResourceDescriptor resourceDescriptor)
        {
            if (resourceDescriptor == null)
            {
                Debug.Assert(false);
                return false;
            }

            string filter = $"resource.type=\"{resourceDescriptor.Type}\"";
            try
            {
                var result =  await _dataSource.Value.ListLogEntriesAsync(filter, pageSize: 1);
                return result?.LogEntries != null && result.LogEntries.Count > 0;
            }
            catch (Exception ex)
            {
                // If exception happens. Keep the type.
                Debug.WriteLine($"Check Resource Type Log Entry failed {ex.ToString()}");
                return true;
            }
        }

        private ProtectedCommand _refreshCommand;
        public ProtectedCommand RefreshCommand => _refreshCommand;
        public string RefreshCommandToolTip => "Get newest log (descending order)";

        private void OnRefreshCommand()
        {
            Reload();
        }

        private object _isLoadingLockObj = new object();
        private bool _isLoading = false;
        private async Task LogLoaddingWrapper(Func<Task> callback)
        {
            lock (_isLoadingLockObj)
            {
                if (_isLoading)
                {
                    Debug.WriteLine($"_isLoading is true.  Fatal error. fix the code.");
                    return;
                }

                ErrorMessage = null;
                Console.WriteLine("Setting _isLoading to true");
                _isLoading = true;
            }


            try
            {
                _canCallNextPage = false;
                RefreshCommand.CanExecuteCommand = false;
                //// TODO: using ... animation or adding it to Resources.
                //LogLoddingProgress = "Loading ... ";

                await callback();
                LogLoddingProgress = string.Empty;
                ErrorMessage = string.Empty;
            }
            catch (DataSourceException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
            }
            finally
            {
                Console.WriteLine("Setting _isLoading to false");
                _isLoading = false;
                CancelLoadingVisibility = Visibility.Collapsed;
                LogLoddingProgress = string.Empty;
                // Disable fetching next page if cancelled or _nextPageToken is empty
                // This is critical otherwise cancelling a "fetch" won't work
                // Because at the time "Cancelled", a scroll down to the bottom event is raised and triggers
                // another automatic NextPage call.
                _canCallNextPage = (!_cancelled && !string.IsNullOrWhiteSpace(_nextPageToken));
                RefreshCommand.CanExecuteCommand = true;
            }
        }

        private static readonly int _defaultPageSize = 100;
        private bool _cancelled = false;
        private async Task LoadLogs(bool firstPage)
        {

            int count = 0;
            _cancelled = false;
            //var reqParams = CurrentRequestParameters();

            while (count < _defaultPageSize && !_cancelled)
            {
                Debug.WriteLine($"LoadLogs, count={count}, firstPage={firstPage}");

                CancelLoadingVisibility = Visibility.Visible;
                LogLoddingProgress = "Loading . . .";

                if (firstPage)
                {
                    firstPage = false;
                    _nextPageToken = null;
                    SetLogs(null, descending:true);
                    SetSelectedChanged(0);
                }

                var results = await _dataSource.Value.ListLogEntriesAsync(
                    pageSize: _defaultPageSize, nextPageToken: _nextPageToken);
                AddLogs(results?.LogEntries);
                _nextPageToken = results.NextPageToken;
                if (results?.LogEntries != null)
                {
                    count += results.LogEntries.Count;
                }

                if (string.IsNullOrWhiteSpace(_nextPageToken))
                {
                    _nextPageToken = null;
                    break;
                }
            }            
        }



        private async Task Reload()
        {
            if (Project == null)
            {
                return;
            }

            await LogLoaddingWrapper(async () => {
                await LoadLogs(firstPage: true);
            });
        }


        public async void LoadNextPage()
        {
            if (!_canCallNextPage || string.IsNullOrWhiteSpace(_nextPageToken))
            {
                return;
            }

            await LogLoaddingWrapper(async () =>
            {
                await LoadLogs(firstPage: false);
            });
        }

        private void ToggleExpandAll()
        {
            if (ToggleExpandHideAll == DataGridRowDetailsVisibilityMode.Collapsed)
            {
                ToggleExpandHideAll = DataGridRowDetailsVisibilityMode.Visible;
            }
            else
            {
                ToggleExpandHideAll = DataGridRowDetailsVisibilityMode.Collapsed;
            }
        }

        private LoggingDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new LoggingDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }
    }
}
