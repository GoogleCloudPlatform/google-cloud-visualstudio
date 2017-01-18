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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// The view model for LogsViewerToolWindow.
    /// </summary>
    public class LogsViewerViewModel : ViewModelBase
    {
        private const int DefaultPageSize = 100;

        private static readonly string[] s_defaultResourceSelections = 
            new string[] {
                "gce_instance",
                "gae_app",
                "global"
            };

        private static readonly LogSeverityItem[] s_logSeveritySelections = 
            new LogSeverityItem[] {
                new LogSeverityItem(LogSeverity.Debug, Resources.LogViewerLogLevelDebugLabel),
                new LogSeverityItem(LogSeverity.Info, Resources.LogViewerLogLevelInfoLabel),
                new LogSeverityItem(LogSeverity.Warning, Resources.LogViewerLogLevelWarningLabel),
                new LogSeverityItem(LogSeverity.Error, Resources.LogViewerLogLevelDebugLabel),
                new LogSeverityItem(LogSeverity.Critical, Resources.LogViewerLogLevelDebugLabel),
                new LogSeverityItem(LogSeverity.Emergency, Resources.LogViewerLogLevelDebugLabel),
                new LogSeverityItem(LogSeverity.All, Resources.LogViewerLogLevelAllLabel)
            };


        /// <summary>
        /// This is the filters combined by all selectors.
        /// </summary>
        private string _filter;
        private MonitoredResourceDescriptor _selectedResource;
        private IList<MonitoredResourceDescriptor> _resourceDescriptors;
        private LogSeverityItem _selectedLogSeverity = s_logSeveritySelections.LastOrDefault();

        private bool _isLoading;
        private Lazy<LoggingDataSource> _dataSource;
        private string _nextPageToken;

        private bool _toggleExpandAllExpanded;
        private bool _isControlEnabled = true;

        private ObservableCollection<LogItem> _logs = new ObservableCollection<LogItem>();

        private string _requestStatusText;
        private string _requestErrorMessage;
        private bool _showRequestErrorMessage;
        private bool _showRequestStatus;
        private bool _showCancelRequestButton;
        private CancellationTokenSource _cancellationTokenSource;
        private TimeZoneInfo _selectedTimeZone = TimeZoneInfo.Local;

        /// <summary>
        /// Gets the list of Log Level items.
        /// </summary>
        public IEnumerable<LogSeverityItem> LogSeverityList => s_logSeveritySelections;

        /// <summary>
        /// Gets or sets the selected log severity value.
        /// </summary>
        public LogSeverityItem SelectedLogSeverity
        {
            get { return _selectedLogSeverity; }
            set
            {
                var old_value = _selectedLogSeverity;
                SetValueAndRaise(ref _selectedLogSeverity, value);
                if (value != null && old_value != value)
                {
                    OnFiltersChanged();
                }
            }
        }

        /// <summary>
        /// Gets all resources types.
        /// </summary>
        public IList<MonitoredResourceDescriptor> ResourceDescriptors
        {
            get { return _resourceDescriptors; }
            private set { SetValueAndRaise(ref _resourceDescriptors, value); }
        }

        /// <summary>
        /// Gets or sets current selected resource types.
        /// </summary>
        public MonitoredResourceDescriptor SelectedResource
        {
            get { return _selectedResource; }
            set
            {
                var old_value = _selectedResource;
                SetValueAndRaise(ref _selectedResource, value);
                // if old_value == null,  this is the first time when ResourceDescriptors is assigned.
                // Ignore this change because the SelectedResource will be set to default value immediately.
                if (old_value != null && value != null && old_value != value)
                {
                    OnFiltersChanged();
                }
            }
        }

        /// <summary>
        /// The time zone selector items.
        /// </summary>
        public IEnumerable<TimeZoneInfo> SystemTimeZones => TimeZoneInfo.GetSystemTimeZones();

        /// <summary>
        /// Selected time zone.
        /// </summary>
        public TimeZoneInfo SelectedTimeZone
        {
            get { return _selectedTimeZone; }
            set
            {
                if (value != _selectedTimeZone)
                {
                    _selectedTimeZone = value;
                    foreach (var log in _logs)
                    {
                        log.ChangeTimeZone(_selectedTimeZone);
                    }

                    LogItemCollection.Refresh();
                }
            }
        }

        /// <summary>
        /// Gets the refresh button command.
        /// </summary>
        public ProtectedCommand RefreshCommand { get; }

        /// <summary>
        /// Gets the account name.
        /// </summary>
        public string Account => CredentialsStore.Default.CurrentAccount?.AccountName ?? "";

        /// <summary>
        /// Gets the project id.
        /// </summary>
        public string Project => CredentialsStore.Default.CurrentProjectId ?? "";

        /// <summary>
        /// Route the expander IsExpanded state to control expand all or collapse all.
        /// </summary>
        public bool ToggleExpandAllExpanded
        {
            get { return _toggleExpandAllExpanded; }
            set
            {
                SetValueAndRaise(ref _toggleExpandAllExpanded, value);
                RaisePropertyChanged(nameof(ToggleExapandAllToolTip));
            }
        }

        /// <summary>
        /// Gets the tool tip for Toggle Expand All button.
        /// </summary>
        public string ToggleExapandAllToolTip => _toggleExpandAllExpanded ?
            Resources.LogViewerCollapseAllTip : Resources.LogViewerExpandAllTip;

        /// <summary>
        /// Gets the LogItem collection
        /// </summary>
        public ListCollectionView LogItemCollection { get; }

        /// <summary>
        /// Gets the cancel request button ICommand interface.
        /// </summary>
        public ProtectedCommand CancelRequestCommand { get; }

        /// <summary>
        /// Gets the cancel request button visibility
        /// </summary>
        public bool ShowCancelRequestButton
        {
            get { return _showCancelRequestButton; }
            private set { SetValueAndRaise(ref _showCancelRequestButton, value); }
        }

        /// <summary>
        /// Gets the request error message visibility.
        /// </summary>
        public bool ShowRequestErrorMessage
        {
            get { return _showRequestErrorMessage; }
            private set { SetValueAndRaise(ref _showRequestErrorMessage, value); }
        }

        /// <summary>
        /// Gets the request error message.
        /// </summary>
        public string RequestErrorMessage
        {
            get { return _requestErrorMessage; }
            private set { SetValueAndRaise(ref _requestErrorMessage, value); }
        }

        /// <summary>
        /// Gets the request status text message.
        /// </summary>
        public string RequestStatusText
        {
            get { return _requestStatusText; }
            private set { SetValueAndRaise(ref _requestStatusText, value); }
        }

        /// <summary>
        /// Gets the request status block visibility. 
        /// It includes the request status text block and cancel request button.
        /// </summary>
        public bool ShowRequestStatus
        {
            get { return _showRequestStatus; }
            private set { SetValueAndRaise(ref _showRequestStatus, value); }
        }

        /// <summary>
        /// Gets if it is making request to remote servers or not.
        /// </summary>
        public bool IsControlEnabled
        {
            get { return _isControlEnabled; }
            private set { SetValueAndRaise(ref _isControlEnabled, value); }
        }

        /// <summary>
        /// Initializes an instance of <seealso cref="LogsViewerViewModel"/> class.
        /// </summary>
        public LogsViewerViewModel()
        {
            _dataSource = new Lazy<LoggingDataSource>(CreateDataSource);
            RefreshCommand = new ProtectedCommand(() => Reload());
            LogItemCollection = new ListCollectionView(_logs);
            LogItemCollection.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LogItem.Date)));
            CancelRequestCommand = new ProtectedCommand(CancelRequest);
        }

        /// <summary>
        /// When a new view model is created and attached to Window, 
        /// invalidate controls and re-load first page of log entries.
        /// </summary>
        public void InvalidateAllProperties()
        {
            if (String.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentAccount?.AccountName) ||
                String.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentProjectId))
            {
                return;
            }

            PopulateResourceTypes();
        }

        /// <summary>
        /// Send request to get logs following prior requests.
        /// </summary>
        public void LoadNextPage()
        {
            if (String.IsNullOrWhiteSpace(_nextPageToken) || String.IsNullOrWhiteSpace(Project))
            {
                return;
            }

            LogLoaddingWrapperAsync(async (cancelToken) => await LoadLogsAsync(cancelToken));
        }

        /// <summary>
        /// Append a set of log entries.
        /// </summary>
        private void AddLogs(IList<LogEntry> logEntries)
        {
            if (logEntries == null)
            {
                return;
            }

            foreach (var log in logEntries)
            {
                _logs.Add(new LogItem(log));
            }
        }

        /// <summary>
        /// Cancel request button command.
        /// </summary>
        private void CancelRequest()
        {
            Debug.WriteLine("Cancel command is called");
            RequestStatusText = Resources.LogViewerRequestCancellingMessage;
            ShowCancelRequestButton = false;
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Show request status bar.
        /// </summary>
        private void InitAndShowRequestStatus()
        {
            ShowRequestErrorMessage = false;
            RequestStatusText = Resources.LogViewerRequestProgressMessage;
            ShowRequestStatus = true;
            ShowCancelRequestButton = true;
        }

        /// <summary>
        /// A wrapper to LoadLogs.
        /// This is to make the try/catch statement conscise and easy to read.
        /// </summary>
        /// <param name="callback">A function to execute.</param>
        private async Task LogLoaddingWrapperAsync(Func<CancellationToken, Task> callback)
        {
            if (_isLoading)
            {
                Debug.WriteLine("_isLoading is true. Skip.");
                return;
            }

            Debug.WriteLine("Setting _isLoading to true");
            _isLoading = true;

            _cancellationTokenSource = new CancellationTokenSource();
            IsControlEnabled = false;
            InitAndShowRequestStatus();
            try
            {
                await callback(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _nextPageToken = null;

                if (ex is TaskCanceledException && _cancellationTokenSource.IsCancellationRequested)
                {
                    // Expected cancellation. Log and continue.
                    Debug.WriteLine("Request was cancelled");
                }
                else if (ex is DataSourceException)
                {
                    ShowRequestErrorMessage = true;
                    RequestErrorMessage = ex.Message;
                }

                throw;
            }
            finally
            {
                IsControlEnabled = true;
                ShowRequestStatus = false;
                Debug.WriteLine("Setting _isLoading to false");
                _isLoading = false;
            }
        }

        /// <summary>
        /// Repeatedly make list log entries request till it gets desired number of logs or it reaches end.
        /// _nextPageToken is used to control if it is getting first page or continuous page.
        /// 
        /// On complex filters, scanning through logs take time. The server returns empty results 
        ///   with a next page token. Continue to send request till some logs are found.
        /// </summary>
        private async Task LoadLogsAsync(CancellationToken cancellationToken)
        {
            int count = 0;
            while (count < DefaultPageSize && !cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"LoadLogs, count={count}, firstPage={_nextPageToken == null}");

                // Here, it does not do pageSize: _defaultPageSize - count, 
                // Because this is requried to use same page size for getting next page. 
                var results = await _dataSource.Value.ListLogEntriesAsync(_filter,
                    pageSize: DefaultPageSize, nextPageToken: _nextPageToken, cancelToken: cancellationToken);
                AddLogs(results?.LogEntries);
                _nextPageToken = results.NextPageToken;
                if (results?.LogEntries != null)
                {
                    count += results.LogEntries.Count;
                }

                if (String.IsNullOrWhiteSpace(_nextPageToken))
                {
                    _nextPageToken = null;
                    break;
                }
            }
        }

        /// <summary>
        /// Send request to get logs using new filters, orders etc.
        /// </summary>
        private void Reload()
        {
            if (String.IsNullOrWhiteSpace(Project))
            {
                Debug.Assert(false, "Project should not be null if the viewer is visible and enabled.");
                return;
            }

            if (_resourceDescriptors?.FirstOrDefault() == null)
            {
                PopulateResourceTypes();
                return;
            }

            LogLoaddingWrapperAsync(async (cancelToken) => {
                _nextPageToken = null;
                _logs.Clear();
                await LoadLogsAsync(cancelToken);
            });
        }

        /// <summary>
        /// Create <seealso cref="LoggingDataSource"/> object with current project id.
        /// </summary>
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

        /// <summary>
        /// Populate resource type selection list.
        /// 
        /// The control flow is as following. 
        ///     1. PopulateResourceTypes().
        ///         1.1 Failed. An error message is displayed. 
        ///              Goto error handling logic.
        ///     2. Set selected resource type.
        ///     3. When selected resource type is changed, it calls Reload().
        ///     
        /// Error handling.
        ///     1. User click Refresh. Refresh button calls Reload().
        ///     2. Reload() checks ResourceDescriptors is null or empty.
        ///     3. Reload calls PopulateResourceTypes() which does a manual retry.
        /// </summary>
        private async void PopulateResourceTypes()
        {
            RequestErrorMessage = null;
            ShowRequestErrorMessage = false;
            IsControlEnabled = false;
            try
            {
                ResourceDescriptors = await _dataSource.Value.GetResourceDescriptorsAsync();
            }
            catch (DataSourceException ex)
            {
                /// If it fails, show a tip, let refresh button retry it.
                RequestErrorMessage = ex.Message;
                ShowRequestErrorMessage = true;
                return;
            }
            finally
            {
                IsControlEnabled = true;
            }

            foreach (var defaultSelection in s_defaultResourceSelections)
            {
                var desc = _resourceDescriptors?.FirstOrDefault(x => x.Type == defaultSelection);
                if (desc != null)
                {
                    SelectedResource = desc;
                    return;
                }
            }

            // Select first one if type of global or gce_instance does not exists.
            SelectedResource = _resourceDescriptors?.FirstOrDefault();
        }

        private void OnFiltersChanged()
        {
            Debug.WriteLine("NotifyFiltersChanged");
            _filter = ComposeSimpleFilters();
            Reload();
        }

        /// <summary>
        /// Aggregate all selections into filter string.
        /// </summary>
        private string ComposeSimpleFilters()
        {
            StringBuilder filter = new StringBuilder();
            if (SelectedResource != null)
            {
                filter.AppendLine($"resource.type=\"{SelectedResource.Type}\"");
            }

            if (SelectedLogSeverity != null && SelectedLogSeverity.Severity != LogSeverity.All)
            {
                filter.AppendLine($"severity>={SelectedLogSeverity.Severity.ToString("G")}");
            }

            return filter.Length > 0 ? filter.ToString() : null;
        }
    }
}
