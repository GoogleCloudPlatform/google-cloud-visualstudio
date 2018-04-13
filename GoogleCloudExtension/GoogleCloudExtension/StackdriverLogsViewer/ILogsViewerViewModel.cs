// Copyright 2018 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace GoogleCloudExtension.StackdriverLogsViewer
{

    /// <summary>
    /// The interface of the view model for <see cref="LogsViewerToolWindow"/>.
    /// </summary>
    public interface ILogsViewerViewModel : IDisposable, IViewModelBase
    {
        /// <summary>
        /// Gets the LogIdList for log id selector binding source.
        /// </summary>
        LogIdsList LogIdList { get; }

        /// <summary>
        /// Gets the DateTimePicker view model object.
        /// </summary>
        DateTimePickerViewModel DateTimePickerModel { get; }

        /// <summary>
        /// Gets the advanced filter help icon button command.
        /// </summary>
        ProtectedCommand AdvancedFilterHelpCommand { get; }

        /// <summary>
        /// Gets the submit advanced filter button command.
        /// </summary>
        ProtectedCommand SubmitAdvancedFilterCommand { get; }

        /// <summary>
        /// The simple text search icon command button.
        /// </summary>
        ProtectedCommand SimpleTextSearchCommand { get; }

        /// <summary>
        /// Gets the toggle advanced and simple filters button Command.
        /// </summary>
        ProtectedCommand FilterSwitchCommand { get; }

        /// <summary>
        /// Gets the command that filters log entris on a detail tree view field value.
        /// </summary>
        ProtectedCommand<ObjectNodeTree> OnDetailTreeNodeFilterCommand { get; }

        /// <summary>
        /// Gets or sets the advanced filter text box content.
        /// </summary>
        string AdvancedFilterText { get; set; }

        /// <summary>
        /// Gets the visbility of advanced filter or simple filter.
        /// </summary>
        bool ShowAdvancedFilter { get; }

        /// <summary>
        /// Set simple search text box content.
        /// </summary>
        string SimpleSearchText { get; set; }

        /// <summary>
        /// Gets the list of Log Level items.
        /// </summary>
        IReadOnlyList<LogSeverityItem> LogSeverityList { get; }

        /// <summary>
        /// Gets the resource type, resource key selector view model.
        /// </summary>
        ResourceTypeMenuViewModel ResourceTypeSelector { get; }

        /// <summary>
        /// Gets or sets the selected log severity value.
        /// </summary>
        LogSeverityItem SelectedLogSeverity { get; set; }

        /// <summary>
        /// The time zone selector items.
        /// </summary>
        IReadOnlyCollection<TimeZoneInfo> SystemTimeZones { get; }

        /// <summary>
        /// Selected time zone.
        /// </summary>
        TimeZoneInfo SelectedTimeZone { get; set; }

        /// <summary>
        /// Gets the refresh button command.
        /// </summary>
        ProtectedCommand RefreshCommand { get; }

        /// <summary>
        /// Gets the account name.
        /// </summary>
        string Account { get; }

        /// <summary>
        /// Gets the project id.
        /// </summary>
        string Project { get; }

        /// <summary>
        /// Route the expander IsExpanded state to control expand all or collapse all.
        /// </summary>
        bool ToggleExpandAllExpanded { get; set; }

        /// <summary>
        /// Gets the tool tip for Toggle Expand All button.
        /// </summary>
        string ToggleExapandAllToolTip { get; }

        /// <summary>
        /// Gets the LogItem collection
        /// </summary>
        ListCollectionView LogItemCollection { get; }

        /// <summary>
        /// Gets the cancel request button ICommand interface.
        /// </summary>
        ProtectedCommand CancelRequestCommand { get; }

        /// <summary>
        /// Gets the cancel request button visibility
        /// </summary>
        bool ShowCancelRequestButton { get; }

        /// <summary>
        /// Gets the request status text message.
        /// </summary>
        string RequestStatusText { get; }

        AsyncProperty AsyncAction { get; set; }

        /// <summary>
        /// Gets the command that responds to auto reload event.
        /// </summary>
        ProtectedCommand OnAutoReloadCommand { get; }

        /// <summary>
        /// The Auto Reload button IsChecked state.
        /// </summary>
        bool IsAutoReloadChecked { get; set; }

        /// <summary>
        /// Gets the auto reload interval in seconds.
        /// </summary>
        uint AutoReloadIntervalSeconds { get; }

        /// <summary>
        /// When a new view model is created and attached to Window,
        /// invalidate controls and re-load first page of log entries.
        /// </summary>
        void InvalidateAllProperties();

        /// <summary>
        /// Send request to get logs following prior requests.
        /// </summary>
        void LoadNextPage();

        /// <summary>
        /// Send an advanced filter to Logs Viewer and display the results.
        /// </summary>
        /// <param name="advancedSearchText">The advance filter in text format.</param>
        void FilterLog(string advancedSearchText);
    }
}
