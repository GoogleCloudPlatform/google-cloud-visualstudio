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
        private const string CloudLogoPath = "CloudExplorer/Resources/logo_cloud.png";

        private static readonly int _defaultPageSize = 100;

        private static readonly Lazy<ImageSource> s_cloud_logo_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(CloudLogoPath));

        private Lazy<LoggingDataSource> _dataSource;
        private string _nextPageToken;

        #region Simple filter area private members
        private ProtectedCommand _refreshCommand;
        #endregion

        #region DataGrid top right corner private members
        private string _selectedDate = string.Empty;
        private bool _toggleExpandAllExpanded = false;
        #endregion

        #region DataGrid private members
        private DataGridRowDetailsVisibilityMode _expandAll = DataGridRowDetailsVisibilityMode.Collapsed;
        private ObservableCollection<LogItem> _logs = new ObservableCollection<LogItem>();
        private ListCollectionView _collectionView;
        private object _collectionViewLock = new object();
        private bool descendingOrder;
        #endregion

        #region Simple filter area public properties
        /// <summary>
        /// Gets the refresh button command.
        /// </summary>
        public ICommand RefreshCommand => _refreshCommand;
        #endregion   

        #region Window top command bar area public properties
        /// <summary>
        /// Gets the Google Cloud Platform Logo image.
        /// </summary>
        public ImageSource CloudLogo => s_cloud_logo_icon.Value;

        /// <summary>
        /// Gets the account name.
        /// </summary>
        public string Account
        {
            get
            {
                return string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentAccount?.AccountName) ? 
                    string.Empty : CredentialsStore.Default?.CurrentAccount?.AccountName;
            }
        }

        /// <summary>
        /// Gets the project id.
        /// </summary>
        public string Project
        {
            get
            {
                return string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentProjectId) ? string.Empty :
                    CredentialsStore.Default.CurrentProjectId;
            }
        }
        #endregion

        #region DataGrid top right corner public properties
        /// <summary>
        /// Route the expander IsExpanded state to control expand all or collapse all.
        /// </summary>
        public bool ToggleExapandAllExpanded
        {
            get { return _toggleExpandAllExpanded; }
            set
            {
                DataGridRowDetailsVisibility =
                    value ? DataGridRowDetailsVisibilityMode.Visible : DataGridRowDetailsVisibilityMode.Collapsed;
                SetValueAndRaise(ref _toggleExpandAllExpanded, value);
                RaisePropertyChanged(nameof(ToggleExapandAllToolTip));
            }
        }

        /// <summary>
        /// Gets the tool tip for Toggle Expand All button.
        /// </summary>
        public string ToggleExapandAllToolTip
        {
            get {
                return _toggleExpandAllExpanded ? Resources.LogViewerCollapseAllTip : Resources.LogViewerExpandAllTip;
            }
        }

        public string SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                if (value != _selectedDate)
                {
                    SetValueAndRaise(ref _selectedDate, value);
                }
            }
        }
        #endregion

        #region DataGrid public properties
        /// <summary>
        /// Gets the LogItem collection
        /// </summary>
        public ListCollectionView LogItemCollection
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

        public DataGridRowDetailsVisibilityMode DataGridRowDetailsVisibility
        {
            get { return _expandAll; }
            set { SetValueAndRaise(ref _expandAll, value); }
        }
        #endregion    

        /// <summary>
        /// Initializes an instance of <seealso cref="LogsViewerViewModel"/> class.
        /// </summary>
        public LogsViewerViewModel()
        {
            _dataSource = new Lazy<LoggingDataSource>(CreateDataSource);
            _refreshCommand = new ProtectedCommand(() => Reload(), canExecuteCommand: false);
        }

        /// <summary>
        /// When a new view model is created and attached to Window, invalidate controls and re-load first page
        /// of log entries.
        /// </summary>
        public async void InvalidateAllControls()
        {
            if (string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentAccount?.AccountName) ||
                string.IsNullOrWhiteSpace(CredentialsStore.Default?.CurrentProjectId))
            {
                return;
            }

            await Reload();
        }

        /// <summary>
        /// Update the current log item date.
        /// </summary>
        /// <param name="item">Current selected log item </param>
        public void SetSelectedChanged(object item)
        {
            var log = item as LogItem;
            if (log == null)
            {
                return;
            }

            SelectedDate = log.Date;
        }

        #region DataGrid public methods
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
                RaisePropertyChanged(nameof(LogItemCollection));
                return;
            }

            AddLogs(logEntries);
        }

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
            }

            RaisePropertyChanged(nameof(LogItemCollection));
        }
        #endregion

        /// <summary>
        /// Send request to get logs following prior requests.
        /// </summary>
        public async void LoadNextPage()
        {
            if (string.IsNullOrWhiteSpace(_nextPageToken) || Project == null)
            {
                return;
            }

            await LogLoaddingWrapper(async () =>
            {
                await LoadLogs(firstPage: false);
            });
        }

        /// <summary>
        /// Disable all filters, refresh buttons when a request is pending.
        /// </summary>
        private void DisableControls()
        {
            _refreshCommand.CanExecuteCommand = false;
        }

        /// <summary>
        /// Enable all controls when request is complete.
        /// </summary>
        private void EnableControls()
        {
            _refreshCommand.CanExecuteCommand = true;
        }

        /// <summary>
        /// A wrapper to LoadLogs.
        /// This is to make the try/catch statement conscise and easy to read.
        /// </summary>
        /// <param name="callback">A function to execute.</param>
        private async Task LogLoaddingWrapper(Func<Task> callback)
        {
            try
            {
                DisableControls();
                await callback();
            }
            finally
            {
                EnableControls();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstPage">
        /// Tell if it is requesting for first page or geting subsequent pages.
        /// On complex filters, scanning through logs take time. The server returns empty results 
        ///   with a next page token. Continue to send request till some logs are found.
        /// </param>
        private async Task LoadLogs(bool firstPage)
        {
            int count = 0;

            while (count < _defaultPageSize)
            {
                Debug.WriteLine($"LoadLogs, count={count}, firstPage={firstPage}");
                if (firstPage)
                {
                    firstPage = false;
                    _nextPageToken = null;

                    // This clears DataGrid contents
                    SetLogs(null, descending:true);
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

        /// <summary>
        /// Send request to get logs using new filters, orders etc.
        /// </summary>
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
