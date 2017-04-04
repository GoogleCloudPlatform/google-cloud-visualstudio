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
using System.ComponentModel;
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
        private const string AdvancedHelpLink = "https://cloud.google.com/logging/docs/view/advanced_filters";
        private const int DefaultPageSize = 100;
        private const uint LogStreamingIntervalInSeconds = 3;

        private static readonly LogSeverityItem[] s_logSeveritySelections =
            new LogSeverityItem[] {
                new LogSeverityItem(LogSeverity.Debug, Resources.LogViewerLogLevelDebugLabel),
                new LogSeverityItem(LogSeverity.Info, Resources.LogViewerLogLevelInfoLabel),
                new LogSeverityItem(LogSeverity.Warning, Resources.LogViewerLogLevelWarningLabel),
                new LogSeverityItem(LogSeverity.Error, Resources.LogViewerLogLevelErrorLabel),
                new LogSeverityItem(LogSeverity.Critical, Resources.LogViewerLogLevelCriticalLabel),
                new LogSeverityItem(LogSeverity.Emergency, Resources.LogViewerLogLevelEmergencyLabel),
                new LogSeverityItem(LogSeverity.All, Resources.LogViewerLogLevelAllLabel)
            };

        /// <summary>
        /// This is the filters combined by all selectors.
        /// </summary>
        private string _filter;

        private LogSeverityItem _selectedLogSeverity = s_logSeveritySelections.LastOrDefault();
        private string _simpleSearchText;
        private string _advacedFilterText;
        private bool _showAdvancedFilter;
        private LogIdsList _logIdList;

        private bool _isLoading;
        private Lazy<LoggingDataSource> _dataSource;
        private string _nextPageToken;
        private DateTime _lastLogTime = DateTime.MinValue;

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
        /// Gets the LogIdList for log id selector binding source.
        /// </summary>
        public LogIdsList LogIdList
        {
            get { return _logIdList; }
            private set { SetValueAndRaise(ref _logIdList, value); }
        }

        /// <summary>
        /// Gets the DateTimePicker view model object.
        /// </summary>
        public DateTimePickerViewModel DateTimePickerModel { get; }

        /// <summary>
        /// Gets the advanced filter help icon button command.
        /// </summary>
        public ProtectedCommand AdvancedFilterHelpCommand { get; }

        /// <summary>
        /// Gets the submit advanced filter button command.
        /// </summary>
        public ProtectedCommand SubmitAdvancedFilterCommand { get; }

        /// <summary>
        /// The simple text search icon command button.
        /// </summary>
        public ProtectedCommand SimpleTextSearchCommand { get; }

        /// <summary>
        /// Gets the toggle advanced and simple filters button Command.
        /// </summary>
        public ProtectedCommand FilterSwitchCommand { get; }

        /// <summary>
        /// Gets the command that filters log entris on a detail tree view field value.
        /// </summary>
        public ProtectedCommand<ObjectNodeTree> OnDetailTreeNodeFilterCommand { get; }

        /// <summary>
        /// Gets or sets the advanced filter text box content.
        /// </summary>
        public string AdvancedFilterText
        {
            get { return _advacedFilterText; }
            set { SetValueAndRaise(ref _advacedFilterText, value); }
        }

        /// <summary>
        /// Gets the visbility of advanced filter or simple filter.
        /// </summary>
        public bool ShowAdvancedFilter
        {
            get { return _showAdvancedFilter; }
            private set { SetValueAndRaise(ref _showAdvancedFilter, value); }
        }

        /// <summary>
        /// Set simple search text box content.
        /// </summary>
        public string SimpleSearchText
        {
            get { return _simpleSearchText; }
            set { SetValueAndRaise(ref _simpleSearchText, value); }
        }

        /// <summary>
        /// Gets the list of Log Level items.
        /// </summary>
        public IEnumerable<LogSeverityItem> LogSeverityList => s_logSeveritySelections;

        /// <summary>
        /// Gets the resource type, resource key selector view model.
        /// </summary>
        public ResourceTypeMenuViewModel ResourceTypeSelector { get; }

        /// <summary>
        /// Gets or sets the selected log severity value.
        /// </summary>
        public LogSeverityItem SelectedLogSeverity
        {
            get { return _selectedLogSeverity; }
            set { SetValueAndRaise(ref _selectedLogSeverity, value); }
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
            set { SetValueAndRaise(ref _selectedTimeZone, value); }
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
        /// Gets the command that responds to auto reload event.
        /// </summary>
        public ProtectedCommand OnAutoReloadCommand { get; }

        /// <summary>
        /// Gets the auto reload interval in seconds.
        /// </summary>
        public uint AutoReloadIntervalSeconds => LogStreamingIntervalInSeconds;

        /// <summary>
        /// Initializes an instance of <seealso cref="LogsViewerViewModel"/> class.
        /// </summary>
        public LogsViewerViewModel()
        {
            _dataSource = new Lazy<LoggingDataSource>(CreateDataSource);
            RefreshCommand = new ProtectedCommand(OnRefreshCommand);
            LogItemCollection = new ListCollectionView(_logs);
            LogItemCollection.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LogItem.Date)));
            CancelRequestCommand = new ProtectedCommand(CancelRequest);
            SimpleTextSearchCommand = new ProtectedCommand(Reload);
            FilterSwitchCommand = new ProtectedCommand(SwapFilter);
            SubmitAdvancedFilterCommand = new ProtectedCommand(Reload);
            AdvancedFilterHelpCommand = new ProtectedCommand(ShowAdvancedFilterHelp);
            DateTimePickerModel = new DateTimePickerViewModel(
                TimeZoneInfo.Local, DateTime.UtcNow, isDescendingOrder: true);
            DateTimePickerModel.DateTimeFilterChange += (sender, e) => Reload();
            PropertyChanged += OnPropertyChanged;
            ResourceTypeSelector = new ResourceTypeMenuViewModel(_dataSource);
            ResourceTypeSelector.PropertyChanged += OnPropertyChanged;
            OnDetailTreeNodeFilterCommand = new ProtectedCommand<ObjectNodeTree>(FilterOnTreeNodeValue);
            OnAutoReloadCommand = new ProtectedCommand(AutoReload);
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

            RequestLogFiltersWrapperAsync(PopulateResourceTypes);
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
        /// Send an advanced filter to Logs Viewer and display the results.
        /// </summary>
        /// <param name="advancedSearchText">The advance filter in text format.</param>
        public void FilterLog(string advancedSearchText)
        {
            if (String.IsNullOrWhiteSpace(advancedSearchText))
            {
                return;
            }

            ShowAdvancedFilter = true;
            StringBuilder filter = new StringBuilder();
            filter.AppendLine(advancedSearchText);
            if (!advancedSearchText.ToLowerInvariant().Contains("timestamp"))
            {
                filter.AppendLine($"timestamp<=\"{DateTime.UtcNow.AddDays(1).ToString("O")}\"");
            }

            AdvancedFilterText = filter.ToString();
            Reload();
        }

        /// <summary>
        /// Detail view is a tree view. 
        /// Each item at tree path is an <seealso cref="ObjectNodeTree"/> object.
        /// The tree view displays the <paramref name="node"/> as name : value pair.
        /// User can click at the "value" to show matching log entries.
        /// 
        /// This method composes a filter on node value, adds it to existing AdvancedFilterText.
        /// The filter has format of root_node_name.node_name...node_name = "node.value". 
        /// Example: jsonPayload.serviceContext.service="frontend"
        /// </summary>
        private void FilterOnTreeNodeValue(ObjectNodeTree node)
        {
            // Firstly compose a new filter line.
            StringBuilder newFilter = new StringBuilder();
            newFilter.Append($"{node.FilterLabel}=\"{node.FilterValue}\"");            
            while ((node = node.Parent).Parent != null)     
            {
                if (!string.IsNullOrWhiteSpace(node.FilterLabel))
                {
                    newFilter.Insert(0, $"{node.FilterLabel}.");
                }
            }

            // Append the new filter line to existing filter text.
            // Or to the composed filter if it is currently showing simple filters.
            if (ShowAdvancedFilter)
            {   
                newFilter.Insert(0, Environment.NewLine); 
                newFilter.Insert(0, AdvancedFilterText);
            }
            else
            {
                newFilter.Insert(0, ComposeSimpleFilters());
            }

            // Show advanced filter.
            AdvancedFilterText = newFilter.ToString();
            ShowAdvancedFilter = true;
            Reload();
        }

        private void OnRefreshCommand()
        {
            DateTimePickerModel.IsDescendingOrder = true;
            DateTimePickerModel.DateTimeUtc = DateTime.UtcNow;
            Reload();
        }

        /// <summary>
        /// Append a set of log entries.
        /// </summary>
        /// <param name="logEntries">The set of log entries to be added to the view.</param>
        /// <param name="autoReload">Indicate if it is the result from auto reload event.</param>
        private void AddLogs(IList<LogEntry> logEntries, bool autoReload = false)
        {
            if (logEntries == null)
            {
                return;
            }

            // Pick the last n items only in case there are too much that makes the viewer too slow.
            var query = logEntries.Select(x => {
                    var item = new LogItem(x, SelectedTimeZone);
                    _lastLogTime = DateTimeUtils.Max(_lastLogTime, item.TimeStamp.ToUniversalTime());
                    return item;
                });

            if (autoReload && DateTimePickerModel.IsDescendingOrder)
            {
                foreach (var item in query.Reverse())
                {
                    Debug.WriteLine($"add entry {item.Entry.Timestamp}");
                    _logs.Insert(0, item);
                }
            }
            else
            {
                foreach (var item in query)
                {
                    _logs.Add(item);
                }
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
        /// <param name="IsCanceButtonVisible">Indicate if the cancel button should be shown</param>
        private void SetServerRequestStartStatus(bool IsCanceButtonVisible = true)
        {
            IsControlEnabled = false;
            RequestErrorMessage = null;
            ShowRequestErrorMessage = false;
            RequestStatusText = Resources.LogViewerRequestProgressMessage;
            ShowRequestStatus = true;
            ShowCancelRequestButton = IsCanceButtonVisible;
        }

        /// <summary>
        /// Hide request status bar, enable controls.
        /// </summary>
        private void SetServerRequestCompleteStatus()
        {
            IsControlEnabled = true;
            ShowRequestStatus = false;
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
            SetServerRequestStartStatus();
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
                else
                {
                    throw;
                }
            }
            finally
            {
                SetServerRequestCompleteStatus();
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
        /// <param name="cancellationToken">A cancellation token. The caller can monitor the cancellation event.</param>
        /// <param name="autoReload">Indicate if the request comes from autoReload event.</param>
        private async Task LoadLogsAsync(CancellationToken cancellationToken, bool autoReload = false)
        {
            var order = DateTimePickerModel.IsDescendingOrder ? "timestamp desc" : "timestamp asc";
            int count = 0;
            while (count < DefaultPageSize && !cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"LoadLogs, count={count}, firstPage={_nextPageToken == null}");

                // Here, it does not do pageSize: _defaultPageSize - count, 
                // Because this is requried to use same page size for getting next page. 
                var results = await _dataSource.Value.ListLogEntriesAsync(_filter, order,
                    pageSize: DefaultPageSize, nextPageToken: _nextPageToken, cancelToken: cancellationToken);
                _nextPageToken = results.NextPageToken;
                if (results?.LogEntries != null)
                {
                    count += results.LogEntries.Count;
                    AddLogs(results.LogEntries, autoReload);
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
            Debug.WriteLine($"Entering Reload(), thread id {Thread.CurrentThread.ManagedThreadId}");

            if (String.IsNullOrWhiteSpace(Project))
            {
                Debug.Assert(false, "Project should not be null if the viewer is visible and enabled.");
                return;
            }

            if (!ResourceTypeSelector.IsSubmenuPopulated)
            {
                RequestLogFiltersWrapperAsync(PopulateResourceTypes);
                Debug.WriteLine("PopulateResourceTypes exit");
                return;
            }

            if (LogIdList == null)
            {
                RequestLogFiltersWrapperAsync(PopulateLogIds);
                Debug.WriteLine("PopulateLogIds exit");
                return;
            }

            _filter = ShowAdvancedFilter ? AdvancedFilterText : ComposeSimpleFilters();

            LogLoaddingWrapperAsync(async (cancelToken) =>
            {
                _nextPageToken = null;
                _logs.Clear();
                _lastLogTime = DateTime.MinValue;
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

        private void ShowAdvancedFilterHelp()
        {
            Process.Start(AdvancedHelpLink);
        }

        private void SwapFilter()
        {
            ShowAdvancedFilter = !ShowAdvancedFilter;
            AdvancedFilterText = ShowAdvancedFilter ? ComposeSimpleFilters() : null;
            SimpleSearchText = null;
            Reload();
        }

        /// <summary>
        /// Returns the current filter for final list log entry request.
        /// </summary>
        /// <returns>
        /// Text filter string.
        /// Or null if it is empty.
        /// </returns>
        private string ComposeTextSearchFilter()
        {
            var splits = StringUtils.SplitStringBySpaceOrQuote(SimpleSearchText);
            if (splits == null || splits.Count() == 0)
            {
                return null;
            }

            return $"({String.Join(" OR ", splits.Select(x => $"\"{x}\""))})";
        }

        /// <summary>
        /// A wrapper for common getting filters API calls.
        /// </summary>
        /// <param name="apiCall">The api call to get resource descriptors or log names etc.</param>
        private async Task RequestLogFiltersWrapperAsync(Func<Task> apiCall)
        {
            SetServerRequestStartStatus(IsCanceButtonVisible: false);
            try
            {
                await apiCall();
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
                SetServerRequestCompleteStatus();
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
        private async Task PopulateResourceTypes() => await ResourceTypeSelector.PopulateResourceTypes();

        /// <summary>
        /// This method uses similar logic as populating resource descriptors.
        /// Refers to <seealso cref="PopulateResourceTypes"/>.
        /// </summary>
        private async Task PopulateLogIds()
        {
            if (ResourceTypeSelector.SelectedMenuItem == null)
            {
                Debug.WriteLine("Code bug, _selectedMenuItem should not be null.");
                return;
            }

            var item = ResourceTypeSelector.SelectedMenuItem as ResourceValueItemViewModel;
            var keys = item == null ? null : new List<string>(new string[] { item.ResourceValue });
            IList<string> logIdRequestResult = await _dataSource.Value.ListProjectLogNamesAsync(ResourceTypeSelector.SelectedTypeNmae, keys);
            LogIdList = new LogIdsList(logIdRequestResult);
            LogIdList.PropertyChanged += OnPropertyChanged;
            Reload();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedTimeZone):
                    OnTimeZoneChanged();
                    break;
                case nameof(LogIdsList.SelectedLogId):
                    if (LogIdList != null)
                    {
                        Reload();
                    }
                    break;
                case nameof(ResourceTypeMenuViewModel.SelectedMenuItem):
                    LogIdList = null;
                    RequestLogFiltersWrapperAsync(PopulateLogIds);
                    break;
                case nameof(SelectedLogSeverity):
                    Reload();
                    break;
                default:
                    break;
            }
        }

        private void OnTimeZoneChanged()
        {
            foreach (var log in _logs)
            {
                log.ChangeTimeZone(SelectedTimeZone);
            }

            LogItemCollection.Refresh();
            DateTimePickerModel.ChangeTimeZone(SelectedTimeZone);
        }

        /// <summary>
        /// Aggregate all selections into filter string.
        /// </summary>
        /// <param name="ignoreTimeStamp">If the value is true,  does not add the timestamp clause.</param>
        private string ComposeSimpleFilters(bool ignoreTimeStamp = false)
        {
            Debug.WriteLine("Entering ComposeSimpleFilters()");

            StringBuilder filter = new StringBuilder();
            if (ResourceTypeSelector.SelectedResourceType != null)
            {
                filter.AppendLine($"resource.type=\"{ResourceTypeSelector.SelectedResourceType.ResourceTypeKeys.Type}\"");
            }

            var valueItem = ResourceTypeSelector.SelectedMenuItem as ResourceValueItemViewModel;
            if (valueItem != null)
            {
                // Example: resource.labels.module_id="my_gae_default_service"
                filter.AppendLine($"resource.labels.{ResourceTypeSelector.SelectedResourceType.GetKeyAt(0)}=\"{valueItem.ResourceValue}\"");
            }

            if (SelectedLogSeverity != null && SelectedLogSeverity.Severity != LogSeverity.All)
            {
                filter.AppendLine($"severity>={SelectedLogSeverity.Severity.ToString("G")}");
            }

            if (!ignoreTimeStamp)
            {
                if (DateTimePickerModel.IsDescendingOrder)
                {
                    if (DateTimePickerModel.DateTimeUtc < DateTime.UtcNow)
                    {
                        filter.AppendLine($"timestamp<=\"{DateTimePickerModel.DateTimeUtc.ToString("O")}\"");
                    }
                }
                else
                {
                    filter.AppendLine($"timestamp>=\"{DateTimePickerModel.DateTimeUtc.ToString("O")}\"");
                }
            }

            if (LogIdList.SelectedLogIdFullName != null)
            {
                filter.AppendLine($"logName=\"{LogIdList.SelectedLogIdFullName}\"");
            }

            var textFilter = ComposeTextSearchFilter();
            if (textFilter != null)
            {
                filter.AppendLine(textFilter);
            }

            return filter.Length > 0 ? filter.ToString() : null;
        }

        private void AutoReload()
        {
            // Possibly, the last auto reload command have not completed.
            if (!IsControlEnabled || _isLoading)
            {
                return;
            }

            // If it is in advanced filter, just do reload. 
            if (ShowAdvancedFilter)
            {
                Reload();
                return;
            }

            // TODO: auto scroll to last item in ascending order.
            AppendNewerLogs();
        }

        private async Task AppendNewerLogs()
        {
            bool createNewQuery = DateTimePickerModel.IsDescendingOrder || _nextPageToken == null;
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                SetServerRequestStartStatus();
                _isLoading = true;

                if (createNewQuery)
                {
                    _nextPageToken = null;
                    Debug.WriteLine($"_lastLogTime log {_lastLogTime}");

                    _filter = ComposeSimpleFilters(ignoreTimeStamp: true)
                        + String.Format($" timestamp>\"{_lastLogTime.ToString("O")}\" ");
                    Debug.WriteLine(_filter);
                }

                do
                {
                    await LoadLogsAsync(_cancellationTokenSource.Token, autoReload: true);
                } while (_nextPageToken != null && !_cancellationTokenSource.IsCancellationRequested);
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
                else
                {
                    throw;
                }
            }
            finally
            {
                SetServerRequestCompleteStatus();
                _isLoading = false;
            }
        }
    }
}
