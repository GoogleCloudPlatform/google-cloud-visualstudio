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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
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
    public class LogsViewerViewModel : ViewModelBase, ILogsViewerViewModel
    {
        private readonly int _toolWindowIdNumber;
        internal const string AdvancedHelpLink = "https://cloud.google.com/logging/docs/view/advanced_filters";
        private const int DefaultPageSize = 100;
        private const uint LogStreamingIntervalInSeconds = 3;
        private const uint MaxLogEntriesCount = 600;
        private const uint LogStreamingDelayInSeconds = 3;

        private static readonly LogSeverityItem[] s_logSeveritySelections =
            {
                new LogSeverityItem(LogSeverity.All, Resources.LogViewerLogLevelAllLabel),
                new LogSeverityItem(LogSeverity.Debug, Resources.LogViewerLogLevelDebugLabel),
                new LogSeverityItem(LogSeverity.Info, Resources.LogViewerLogLevelInfoLabel),
                new LogSeverityItem(LogSeverity.Warning, Resources.LogViewerLogLevelWarningLabel),
                new LogSeverityItem(LogSeverity.Error, Resources.LogViewerLogLevelErrorLabel),
                new LogSeverityItem(LogSeverity.Critical, Resources.LogViewerLogLevelCriticalLabel),
                new LogSeverityItem(LogSeverity.Emergency, Resources.LogViewerLogLevelEmergencyLabel)
            };


        private readonly ObservableCollection<LogItem> _logs = new ObservableCollection<LogItem>();
        private readonly Lazy<LoggingDataSource> _dataSourceLazy = new Lazy<LoggingDataSource>(CreateDataSource);

        /// <summary>
        /// This is the filters combined by all selectors.
        /// </summary>
        private string _filter;

        private LogSeverityItem _selectedLogSeverity = s_logSeveritySelections.FirstOrDefault();
        private string _simpleSearchText;
        private string _advacedFilterText;
        private bool _showAdvancedFilter;
        private LogIdsList _logIdList;
        private bool _isAutoReloadChecked;
        private string _nextPageToken;
        private LogItem _latestLogItem;
        private readonly IGoogleCloudExtensionPackage _package;

        private bool _toggleExpandAllExpanded;

        private TimeZoneInfo _selectedTimeZone = TimeZoneInfo.Local;

        /// <summary>
        /// For testing.
        /// </summary>
        private readonly ILoggingDataSource _dataSourceOverride = null;

        private AsyncProperty _asyncAction = new AsyncProperty(Task.FromResult(true));
        private CancellationTokenSource _cancellationTokenSource;

        internal Func<string, Process> StartProcess { private get; set; } = Process.Start;

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
        /// Indicates whether the view is visible or not
        /// </summary>
        public bool IsVisibleUnbound { get; set; }

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
        public IReadOnlyList<LogSeverityItem> LogSeverityList => s_logSeveritySelections;

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
            set
            {
                SetValueAndRaise(ref _selectedLogSeverity, value);
                AsyncAction = new AsyncProperty(ReloadAsync());
            }
        }

        /// <summary>
        /// The time zone selector items.
        /// </summary>
        public IReadOnlyCollection<TimeZoneInfo> SystemTimeZones => TimeZoneInfo.GetSystemTimeZones();

        /// <summary>
        /// Selected time zone.
        /// </summary>
        public TimeZoneInfo SelectedTimeZone
        {
            get { return _selectedTimeZone; }
            set
            {
                SetValueAndRaise(ref _selectedTimeZone, value);
                OnTimeZoneChanged();
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
        public bool ShowCancelRequestButton => AsyncAction.IsPending && CancellationTokenSource != null &&
            !CancellationTokenSource.IsCancellationRequested;

        /// <summary>
        /// Gets the request status text message.
        /// </summary>
        public string RequestStatusText
        {
            get
            {
                if (AsyncAction.IsPending)
                {
                    if (CancellationTokenSource?.IsCancellationRequested ?? false)
                    {
                        return Resources.LogViewerRequestCancellingMessage;
                    }
                    else
                    {
                        return Resources.LogViewerRequestProgressMessage;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public AsyncProperty AsyncAction
        {
            get { return _asyncAction; }
            set
            {
                SetValueAndRaise(ref _asyncAction, value);
                RaisePropertyChanged(nameof(RequestStatusText));
                RaisePropertyChanged(nameof(ShowCancelRequestButton));
                AsyncAction.PropertyChanged += (sender, args) =>
                {
                    if (string.IsNullOrWhiteSpace(args.PropertyName) ||
                        args.PropertyName == nameof(AsyncProperty.IsPending))
                    {
                        RaisePropertyChanged(nameof(RequestStatusText));
                        RaisePropertyChanged(nameof(ShowCancelRequestButton));
                    }
                };
            }
        }

        private CancellationTokenSource CancellationTokenSource
        {
            get { return _cancellationTokenSource; }
            set
            {
                SetValueAndRaise(ref _cancellationTokenSource, value);
                RaisePropertyChanged(nameof(ShowCancelRequestButton));
                RaisePropertyChanged(nameof(RequestStatusText));
                CancellationTokenSource?.Token.Register(
                    () =>
                    {
                        RaisePropertyChanged(nameof(ShowCancelRequestButton));
                        RaisePropertyChanged(nameof(RequestStatusText));
                    });
            }
        }

        /// <summary>
        /// Gets the command that responds to auto reload event.
        /// </summary>
        public ProtectedCommand OnAutoReloadCommand { get; }

        /// <summary>
        /// The Auto Reload button IsChecked state.
        /// </summary>
        public bool IsAutoReloadChecked
        {
            get { return _isAutoReloadChecked; }
            set { SetValueAndRaise(ref _isAutoReloadChecked, value); }
        }

        /// <summary>
        /// Gets the auto reload interval in seconds.
        /// </summary>
        public uint AutoReloadIntervalSeconds => LogStreamingIntervalInSeconds;

        /// <summary>
        /// The LoggingDataSource to use.
        /// </summary>
        private ILoggingDataSource DataSource => _dataSourceOverride ?? _dataSourceLazy.Value;

        /// <summary>
        /// For testing.
        /// </summary>
        /// <param name="dataSource">
        /// The mocked data source to use instead the value from <see cref="CreateDataSource"/>.
        /// </param>
        internal LogsViewerViewModel(ILoggingDataSource dataSource) : this(0)
        {
            _dataSourceOverride = dataSource;
        }

        /// <summary>
        /// Initializes an instance of <seealso cref="LogsViewerViewModel"/> class.
        /// </summary>
        /// <param name="toolWindowIdNumber"></param>
        public LogsViewerViewModel(int toolWindowIdNumber)
        {
            IsVisibleUnbound = true;
            _package = GoogleCloudExtensionPackage.Instance;
            _toolWindowIdNumber = toolWindowIdNumber;
            RefreshCommand = new ProtectedCommand(OnRefreshCommand);
            LogItemCollection = new ListCollectionView(_logs);
            LogItemCollection.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LogItem.Date)));
            CancelRequestCommand = new ProtectedCommand(CancelRequest);
            SimpleTextSearchCommand = new ProtectedCommand(() =>
            {
                EventsReporterWrapper.ReportEvent(LogsViewerSimpleTextSearchEvent.Create());
                AsyncAction = new AsyncProperty(ReloadAsync());
            });
            FilterSwitchCommand = new ProtectedCommand(SwapFilter);
            SubmitAdvancedFilterCommand = new ProtectedCommand(() =>
            {
                EventsReporterWrapper.ReportEvent(LogsViewerAdvancedFilterEvent.Create());
                AsyncAction = new AsyncProperty(ReloadAsync());
            });
            AdvancedFilterHelpCommand = new ProtectedCommand(ShowAdvancedFilterHelp);
            DateTimePickerModel = new DateTimePickerViewModel(
                TimeZoneInfo.Local, DateTime.UtcNow, isDescendingOrder: true);
            DateTimePickerModel.DateTimeFilterChange += (sender, e) => AsyncAction = new AsyncProperty(ReloadAsync());
            ResourceTypeSelector = new ResourceTypeMenuViewModel(() => DataSource);
            ResourceTypeSelector.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == null ||
                    args.PropertyName == nameof(ResourceTypeMenuViewModel.SelectedMenuItem))
                {
                    LogIdList = null;
                    AsyncAction = new AsyncProperty(ReloadAsync());
                }
            };

            OnDetailTreeNodeFilterCommand = new ProtectedCommand<ObjectNodeTree>(FilterOnTreeNodeValue);
            OnAutoReloadCommand = new ProtectedCommand(AutoReload);
        }

        /// <summary>
        /// When a new view model is created and attached to Window,
        /// invalidate controls and re-load first page of log entries.
        /// </summary>
        public void InvalidateAllProperties()
        {
            if (string.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentAccount?.AccountName) ||
                string.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentProjectId))
            {
                return;
            }

            AsyncAction = new AsyncProperty(ReloadAsync());
        }

        /// <summary>
        /// Send request to get logs following prior requests.
        /// </summary>
        public void LoadNextPage()
        {
            IsAutoReloadChecked = false;
            if (string.IsNullOrWhiteSpace(_nextPageToken) || string.IsNullOrWhiteSpace(Project) ||
                AsyncAction.IsPending)
            {
                return;
            }

            AsyncAction = new AsyncProperty(LoadLogsAsync());
        }

        /// <summary>
        /// Send an advanced filter to Logs Viewer and display the results.
        /// </summary>
        /// <param name="advancedSearchText">The advance filter in text format.</param>
        public void FilterLog(string advancedSearchText)
        {
            IsAutoReloadChecked = false;
            if (string.IsNullOrWhiteSpace(advancedSearchText))
            {
                return;
            }

            ShowAdvancedFilter = true;
            var filter = new StringBuilder();
            filter.AppendLine(advancedSearchText);
            if (!advancedSearchText.ToLowerInvariant().Contains("timestamp"))
            {
                filter.AppendLine($"timestamp<=\"{DateTime.UtcNow.AddDays(1):O}\"");
            }

            AdvancedFilterText = filter.ToString();
            AsyncAction = new AsyncProperty(ReloadAsync());
        }

        /// <summary>
        /// Dispose the object, implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            OnAutoReloadCommand.CanExecuteCommand = false;
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
            IsAutoReloadChecked = false;

            // Firstly compose a new filter line.
            var newFilter = new StringBuilder();
            newFilter.Append($"{node.FilterLabel}=\"{node.FilterValue}\"");
            while ((node = node.Parent)?.Parent != null)
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

            var newWindow = ToolWindowCommandUtils.AddToolWindow<LogsViewerToolWindow>();
            newWindow.ViewModel.FilterLog(newFilter.ToString());
        }

        private void OnRefreshCommand()
        {
            DateTimePickerModel.IsDescendingOrder = true;
            DateTimePickerModel.DateTimeUtc = DateTime.UtcNow;
            AsyncAction = new AsyncProperty(ReloadAsync());
        }

        /// <summary>
        /// Append a set of log entries.
        /// </summary>
        /// <param name="logEntries">The set of log entries to be added to the view.</param>
        /// <param name="autoReload">Indicate if it is the result from auto reload event.</param>
        private void AddLogs(IList<LogEntry> logEntries, bool autoReload = false)
        {
            if (logEntries == null || logEntries.Count == 0)
            {
                return;
            }

            IEnumerable<LogItem> query =
                logEntries.Select(x => new LogItem(x, SelectedTimeZone, _toolWindowIdNumber)).ToList();
            if (autoReload && DateTimePickerModel.IsDescendingOrder)
            {
                foreach (LogItem item in query.Reverse())
                {
                    Debug.WriteLine($"add entry {item.Entry.Timestamp}");
                    _logs.Insert(0, item);
                }
            }
            else
            {
                foreach (LogItem item in query)
                {
                    _logs.Add(item);
                }
            }

            _latestLogItem = DateTimePickerModel.IsDescendingOrder ? query.First() : query.Last();
        }

        /// <summary>
        /// Cancel request button command.
        /// </summary>
        private void CancelRequest()
        {
            CancellationTokenSource?.Cancel();
            EventsReporterWrapper.ReportEvent(LogsViewerCancelRequestEvent.Create());
        }

        /// <summary>
        /// Repeatedly make list log entries request till it gets desired number of logs or it reaches end.
        /// _nextPageToken is used to control if it is getting first page or continuous page.
        /// 
        /// On complex filters, scanning through logs take time. The server returns empty results
        ///   with a next page token. Continue to send request till some logs are found.
        /// </summary>
        /// <param name="autoReload">Indicate if the request comes from autoReload event.</param>
        private async Task LoadLogsAsync(bool autoReload = false)
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;
            CancellationTokenSource = tokenSource;

            if (_logs.Count >= MaxLogEntriesCount)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.LogViewerResultSetTooLargeMessage,
                    title: Resources.UiDefaultPromptTitle);
                CancellationTokenSource?.Cancel();
                return;
            }

            try
            {
                DateTime startTimestamp = DateTime.Now;

                string order = DateTimePickerModel.IsDescendingOrder ? "timestamp desc" : "timestamp asc";
                int count = 0;
                do
                {
                    // Here, it does not do pageSize: _defaultPageSize - count,
                    // Because this is requried to use same page size for getting next page.
                    LogEntryRequestResult results = await DataSource.ListLogEntriesAsync(
                        _filter, order,
                        pageSize: DefaultPageSize, nextPageToken: _nextPageToken, cancelToken: cancellationToken);
                    _nextPageToken = results.NextPageToken;
                    if (results.LogEntries != null)
                    {
                        count += results.LogEntries.Count;
                        AddLogs(results.LogEntries, autoReload);
                    }

                    if (string.IsNullOrWhiteSpace(_nextPageToken))
                    {
                        _nextPageToken = null;
                    }
                } while (count < DefaultPageSize && !cancellationToken.IsCancellationRequested &&
                    _nextPageToken != null);

                EventsReporterWrapper.ReportEvent(LogsViewerLogsLoadedEvent.Create(CommandStatus.Success, DateTime.Now - startTimestamp));
            }
            catch (Exception)
            {
                _nextPageToken = null;
                EventsReporterWrapper.ReportEvent(LogsViewerLogsLoadedEvent.Create(CommandStatus.Failure));
                throw;
            }
        }

        /// <summary>
        /// Send request to get logs using new filters, orders etc.
        /// </summary>
        private async Task ReloadAsync()
        {
            Debug.WriteLine($"Entering Reload(), thread id {Thread.CurrentThread.ManagedThreadId}");
            _latestLogItem = null;

            if (string.IsNullOrWhiteSpace(Project))
            {
                Debug.Assert(false, "Project should not be null if the viewer is visible and enabled.");
            }

            if (!ResourceTypeSelector.IsSubmenuPopulated)
            {
                await PopulateResourceTypesAsync();
            }

            if (LogIdList == null)
            {
                await PopulateLogIdsAsync();
            }

            if (_latestLogItem == null)
            {
                _filter = ShowAdvancedFilter ? AdvancedFilterText : ComposeSimpleFilters();
                _nextPageToken = null;
                _logs.Clear();

                await LoadLogsAsync();
            }
        }

        /// <summary>
        /// Create <seealso cref="LoggingDataSource"/> object with current project id.
        /// </summary>
        private static LoggingDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new LoggingDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.Instance.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }

        private void ShowAdvancedFilterHelp()
        {
            EventsReporterWrapper.ReportEvent(LogsViewerShowAdvancedFilterHelpEvent.Create());
            StartProcess(AdvancedHelpLink);
        }

        private void SwapFilter()
        {
            ShowAdvancedFilter = !ShowAdvancedFilter;
            AdvancedFilterText = ShowAdvancedFilter ? ComposeSimpleFilters() : null;
            SimpleSearchText = null;
            AsyncAction = new AsyncProperty(ReloadAsync());
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
            List<string> splits = StringUtils.SplitStringBySpaceOrQuote(SimpleSearchText);
            if (splits?.Count >= 0)
            {
                return $"({string.Join(" OR ", splits.Select(x => $"\"{x}\""))})";
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Populate resource type selection list.
        /// 
        /// The control flow is as follows.
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
        private async Task PopulateResourceTypesAsync()
        {
            CancellationTokenSource = null;
            await ResourceTypeSelector.PopulateResourceTypesAsync();
        }

        /// <summary>
        /// This method uses similar logic as populating resource descriptors.
        /// Refers to <seealso cref="PopulateResourceTypesAsync"/>.
        /// </summary>
        private async Task PopulateLogIdsAsync()
        {
            if (ResourceTypeSelector.SelectedMenuItem == null)
            {
                Debug.WriteLine("Code bug, _selectedMenuItem should not be null.");
                return;
            }

            CancellationTokenSource = null;
            var item = ResourceTypeSelector.SelectedMenuItem as ResourceValueItemViewModel;
            List<string> keys = item == null ? null : new List<string> { item.ResourceValue };
            IList<string> logIdRequestResult =
                await DataSource.ListProjectLogNamesAsync(ResourceTypeSelector.SelectedTypeNmae, keys);
            LogIdList = new LogIdsList(logIdRequestResult);
            LogIdList.PropertyChanged += (sender, args) => AsyncAction = new AsyncProperty(ReloadAsync());
        }

        private void OnTimeZoneChanged()
        {
            foreach (LogItem log in _logs)
            {
                log.ChangeTimeZone(SelectedTimeZone);
            }

            LogItemCollection.Refresh();
            DateTimePickerModel.ChangeTimeZone(SelectedTimeZone);
        }

        /// <summary>
        /// Aggregate all selections into filter string.
        /// </summary>
        private string ComposeSimpleFilters()
        {
            Debug.WriteLine("Entering ComposeSimpleFilters()");

            var filter = new StringBuilder();
            if (ResourceTypeSelector.SelectedResourceType != null)
            {
                filter.AppendLine($"resource.type=\"{ResourceTypeSelector.SelectedResourceType.ResourceTypeKeys.Type}\"");

                var valueItem = ResourceTypeSelector.SelectedMenuItem as ResourceValueItemViewModel;
                if (valueItem != null)
                {
                    // Example: resource.labels.module_id="my_gae_default_service"
                    filter.AppendLine(
                        $"resource.labels.{ResourceTypeSelector.SelectedResourceType.GetKeyAt(0)}=\"{valueItem.ResourceValue}\"");
                }
            }

            if (SelectedLogSeverity != null && SelectedLogSeverity.Severity != LogSeverity.All)
            {
                filter.AppendLine($"severity>={SelectedLogSeverity.Severity:G}");
            }

            if (DateTimePickerModel.IsDescendingOrder)
            {
                if (DateTimePickerModel.DateTimeUtc < DateTime.UtcNow)
                {
                    filter.AppendLine($"timestamp<=\"{DateTimePickerModel.DateTimeUtc:O}\"");
                }
            }
            else
            {
                filter.AppendLine($"timestamp>=\"{DateTimePickerModel.DateTimeUtc:O}\"");
            }

            if (LogIdList?.SelectedLogIdFullName != null)
            {
                filter.AppendLine($"logName=\"{LogIdList.SelectedLogIdFullName}\"");
            }

            string textFilter = ComposeTextSearchFilter();
            if (textFilter != null)
            {
                filter.AppendLine(textFilter);
            }

            return filter.Length > 0 ? filter.ToString() : null;
        }

        private void AutoReload()
        {
            // Possibly, the last auto reload command have not completed.
            if (AsyncAction.IsPending || !IsAutoReloadChecked)
            {
                return;
            }

            // If the view is not visible, don't reload
            if (!IsVisibleUnbound || (_package != null && !_package.IsWindowActive()))
            {
                return;
            }

            // If it is in advanced filter, just do reload.
            if (ShowAdvancedFilter)
            {
                AsyncAction = new AsyncProperty(ReloadAsync());
            }
            else
            {
                // TODO: auto scroll to last item in ascending order.
                AsyncAction = new AsyncProperty(AppendNewerLogsAsync());
            }
        }

        private async Task AppendNewerLogsAsync()
        {
            bool createNewQuery = DateTimePickerModel.IsDescendingOrder || _nextPageToken == null;

            if (createNewQuery)
            {
                _nextPageToken = null;
                Debug.WriteLine($"_latestLogItem is {_latestLogItem?.TimeStamp}, {_latestLogItem?.Message}");
                if (DateTimePickerModel.IsDescendingOrder)
                {
                    DateTimePickerModel.DateTimeUtc = DateTime.UtcNow;
                }

                var filter = new StringBuilder(ComposeSimpleFilters());
                filter.AppendLine($"timestamp<\"{DateTime.UtcNow.AddSeconds(-LogStreamingDelayInSeconds):O}\"");
                if (_latestLogItem != null)
                {
                    string dateTimeString = _latestLogItem.Entry.Timestamp as string ??
                        _latestLogItem.TimeStamp.ToUniversalTime().ToString("O");
                    filter.AppendLine($" (timestamp>\"{dateTimeString}\" OR (timestamp=\"{dateTimeString}\"  insertId>\"{_latestLogItem.Entry.InsertId}\") ) ");
                }
                _filter = filter.ToString();
                Debug.WriteLine(_filter);
            }

            do
            {
                await LoadLogsAsync(true);
            } while (_nextPageToken != null && !CancellationTokenSource.IsCancellationRequested);

            if (CancellationTokenSource.IsCancellationRequested)
            {
                IsAutoReloadChecked = false;
            }
        }
    }
}
