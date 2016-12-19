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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Filters related controls view model of LogsViewerViewModel
    /// </summary>
    public partial class LogsViewerViewModel : ViewModelBase
    {
        private const string AdvancedHelpLink = "https://cloud.google.com/logging/docs/view/advanced_filters";

        private static readonly string[] s_defaultResourceSelections = new string[] { "global", "gce_instance" };
        private static readonly string[] s_logSeverityList =
            new string[] { "DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL", "EMERGENCY",
                Resources.LogViewerAllLogLevelSelection};

        /// <summary>
        /// This is the filters combined by all selectors.
        /// </summary>
        private string _filter;

        private LogIdsList _logIdList;
        private MonitoredResourceDescriptor _selectedResource;
        private IList<MonitoredResourceDescriptor> _resourceDescriptors;
        private string _selectedLogSeverity = Resources.LogViewerAllLogLevelSelection;
        private string _simpleSearchText;
        private string _advacedFilterText;
        private bool _showAdvancedFilter = false;

        /// <summary>
        /// Gets the LogIdList for log id selector binding source.
        /// </summary>
        public LogIdsList LogIdList {
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
        /// Gets the toggle advanced and simple filters button Command.
        /// </summary>
        public ProtectedCommand FilterSwitchCommand { get; }

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
        /// Gets the list of Log Level selectors.
        /// </summary>
        public IEnumerable<string> LogSeverityList => s_logSeverityList;

        /// <summary>
        /// The simple text search icon command button.
        /// </summary>
        public ProtectedCommand SimpleTextSearchCommand { get; }

        /// <summary>
        /// Set simple search text box content.
        /// </summary>
        public string SimpleSearchText
        { 
            set
            {
                // This disables loading next page that is based on prior filters. 
                _nextPageToken = null;

                _simpleSearchText = value;
                if (String.IsNullOrWhiteSpace(_simpleSearchText))
                {
                    LogItemCollection.Filter = null;
                    SimpleTextSearchCommand.CanExecuteCommand = false;
                    return;
                }

                SimpleTextSearchCommand.CanExecuteCommand = true;
                DataGridQuickSearch(value);
            }
        }

        /// <summary>
        /// Gets or sets the log severity current selection.
        /// </summary>
        public string SelectedLogSeverity
        {
            get { return _selectedLogSeverity; }
            set
            {
                if (value != null && _selectedLogSeverity != value)
                {
                    _selectedLogSeverity = value;
                    Reload();
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
                if (value != null && _selectedResource != value)
                {
                    SetValueAndRaise(ref _selectedResource, value);
                    Reload();
                }
            }
        }

        private void ShowAdvancedFilterHelp()
        {
            Process.Start(new ProcessStartInfo(AdvancedHelpLink));
        }

        private void SwapFilter()
        {
            ShowAdvancedFilter = !_showAdvancedFilter;
            AdvancedFilterText = _showAdvancedFilter ? ComposeSimpleFilters() : null;
        }

        /// <summary>
        /// Returns the current filter for final list log entry request.
        /// </summary>
        /// <returns>
        /// A text filter string.
        /// Or null if it is empty.
        /// </returns>
        private string ComposeTextSearchFilter()
        {
            var splits = _simpleSearchText?.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (splits == null || splits.Count() == 0)
            {
                return null;
            }

            return $"( {String.Join(" OR ", splits.Select(x => $"\"{x}\""))} )";
        }

        /// <summary>
        /// Apply text search filter at data grid collection view.
        /// </summary>
        private void DataGridQuickSearch(string searchText)
        {
            var splits = searchText.Split(new Char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            LogItemCollection.Filter = new Predicate<object>(item => {
                foreach (var subFilter in splits)
                {
                    if (((LogItem)item).Message.IndexOf(subFilter, StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        return true;
                    }
                }

                return false;
            });
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
        ///     1. User click Refresh. Refrsh button calls Reload().
        ///     2. Reload() checks ResourceDescriptors is null or empty.
        ///     3. Reload calls PopulateResourceTypes() which does a manual retry.
        /// </summary>
        private async Task PopulateResourceTypes()
        {
            ResourceDescriptors = await _dataSource.Value.GetResourceDescriptorsAsync();
            foreach (var defaultSelection in s_defaultResourceSelections)
            {
                var desc = _resourceDescriptors?.First(x => x.Type == defaultSelection);
                if (desc != null)
                {
                    SelectedResource = desc;
                    return;
                }
            }

            // Select first one if type of global or gce_instance does not exists.
            SelectedResource = _resourceDescriptors?[0];
        }

        /// <summary>
        /// This method uses similar logic as populating resource descriptors.
        /// Refers to <seealso cref="PopulateResourceTypes"/>.
        /// </summary>
        private async Task PopulateLogIds()
        {
            IList<string> logIdRequestResult = null;
            logIdRequestResult = await _dataSource.Value.ListProjectLogNamesAsync();
            LogIdList = new LogIdsList(logIdRequestResult, Reload);
            Reload();
        }

        /// <summary>
        /// Aggregate all selections into filter string.
        /// </summary>
        private string ComposeSimpleFilters()
        {
            StringBuilder filter = new StringBuilder();
            if (_selectedResource != null)
            {
                filter.AppendLine($"resource.type=\"{_selectedResource.Type}\"");
            }

            if (_selectedLogSeverity != null && _selectedLogSeverity != Resources.LogViewerAllLogLevelSelection)
            {
                filter.AppendLine($"severity>={_selectedLogSeverity}");
            }

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

            Debug.Assert(LogIdList != null, "LogIDList should not be null.");
            if (LogIdList.SelectedLogIdFullName != null)
            {
                filter.Append($"logName=\"{LogIdList.SelectedLogIdFullName}\"");
            }

            var textFilter = ComposeTextSearchFilter();
            if (textFilter != null)
            {
                filter.AppendLine(textFilter);
            }

            return filter.Length > 0 ? filter.ToString() : null;
        }
    }
}
