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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// View model to <seealso cref="ErrorReportingDetailToolWindowControl"/> control.
    /// </summary>
    public class ErrorReportingDetailViewModel : ViewModelBase
    {
        private Lazy<StackdriverErrorReportingDataSource> _datasource;
        private bool _isGroupLoading;
        private bool _isEventLoading;
        private bool _isControlEnabled = true;
        private bool _showException;
        private string _exceptionString;
        private bool _isAccountReset;
        private TimeRangeItem _selectedTimeRange;
        private Lazy<List<TimeRangeItem>> _timeRangeItemList = new Lazy<List<TimeRangeItem>>(TimeRangeItem.CreateTimeRanges);

        /// <summary>
        /// Indicate the Google account is set.
        /// </summary>
        public bool IsAccountReset
        {
            get { return _isAccountReset; }
            set { SetValueAndRaise(ref _isAccountReset, value); }
        }

        /// <summary>
        /// The exception message to show as error message.
        /// </summary>
        public string ExceptionString
        {
            get { return _exceptionString; }
            set { SetValueAndRaise(ref _exceptionString, value); }
        }

        /// <summary>
        /// Indicate to show or hide the exception message.
        /// </summary>
        public bool ShowException
        {
            get { return _showException; }
            set { SetValueAndRaise(ref _showException, value); }
        }

        /// <summary>
        /// Indicate if the entire window is enabled.
        /// </summary>
        public bool IsControlEnabled
        {
            get { return _isControlEnabled; }
            set { SetValueAndRaise(ref _isControlEnabled, value); }
        }

        /// <summary>
        /// It is loading error group data.
        /// </summary>
        public bool IsGroupLoading
        {
            get { return _isGroupLoading; }
            set { SetValueAndRaise(ref _isGroupLoading, value); }
        }

        /// <summary>
        /// It is loading error event list.
        /// </summary>
        public bool IsEventLoading
        {
            get { return _isEventLoading; }
            set { SetValueAndRaise(ref _isEventLoading, value); }
        }

        /// <summary>
        /// Gets or sets the error group.
        /// </summary>
        public ErrorGroupItem GroupItem { get; private set; }

        /// <summary>
        /// Group item message.
        /// </summary>
        public string Message => GroupItem?.Message;

        /// <summary>
        /// Group item stack message.
        /// </summary>
        public string Stack => GroupItem?.ErrorGroup?.Representative?.Message;

        /// <summary>
        /// Group item stack.
        /// </summary>
        public string StakcSummary => GroupItem?.FirstStackFrame;

        /// <summary>
        /// The data collection of event item list.
        /// </summary>
        public CollectionView EventItemCollection { get; private set; }

        /// <summary>
        /// Sets the currently selected time range.
        /// </summary>
        public TimeRangeItem SelectedTimeRangeItem
        {
            get { return _selectedTimeRange; }
            set
            {
                SetValueAndRaise(ref _selectedTimeRange, value);
                if (value != null)
                {
                    UpdateGroupAndEventAsync();
                }
            }
        }

        /// <summary>
        /// Gets the list of time range items.
        /// </summary>
        public IEnumerable<TimeRangeItem> AllTimeRangeItems => _timeRangeItemList.Value;

        /// <summary>
        /// Go back to overview window command.
        /// </summary>
        public ProtectedCommand OnBackToOverViewCommand { get; }

        /// <summary>
        /// Initializes a new instance of <seealso cref="ErrorReportingDetailViewModel"/> class.
        /// </summary>
        public ErrorReportingDetailViewModel()
        {
            OnBackToOverViewCommand = new ProtectedCommand(() => ToolWindowCommandUtils.ShowToolWindow<ErrorReportingToolWindow>());
            _datasource = new Lazy<StackdriverErrorReportingDataSource>(CreateDataSource);
        }

        /// <summary>
        /// Hide detail view content when project id is changed.s
        /// </summary>
        public void OnCurrentProjectChanged()
        {
            IsAccountReset = true;
            _datasource = new Lazy<StackdriverErrorReportingDataSource>(CreateDataSource);
        }

        /// <summary>
        /// Update detail view with a new <paramref name="errorGroupItem"/>.
        /// </summary>
        /// <param name="errorGroupItem">The error group item showing in the detail view.</param>
        /// <param name="groupSelectedTimeRangeItem">The selected time range.</param>
        public void UpdateView(ErrorGroupItem errorGroupItem, TimeRangeItem groupSelectedTimeRangeItem)
        {
            if (errorGroupItem == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(errorGroupItem)));
            }
            if (groupSelectedTimeRangeItem == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(groupSelectedTimeRangeItem)));
            }

            IsAccountReset = false;
            GroupItem = errorGroupItem;
            if (SelectedTimeRangeItem != null && groupSelectedTimeRangeItem.EventTimeRange == SelectedTimeRangeItem.EventTimeRange)
            {
                UpdateEventAsync();
                RaiseAllPropertyChanged();
            }
            else
            {
                // This will end up calling UpdateView() too. 
                SelectedTimeRangeItem = AllTimeRangeItems.First(x => x.GroupTimeRange == groupSelectedTimeRangeItem.GroupTimeRange);
                UpdateGroupAndEventAsync();
            }
        }

        /// <summary>
        /// Load new even group data.
        /// </summary>
        private async Task UpdateEventGroupAsync()
        {
            if (GroupItem == null)
            {
                Debug.Assert(false, "UpdateEventGroupAsync, GroupItem is null.");
                return;
            }

            IsGroupLoading = true;
            ShowException = false;
            try
            {
                var groups = await _datasource.Value?.GetPageOfGroupStatusAsync(
                    SelectedTimeRangeItem.GroupTimeRange,
                    SelectedTimeRangeItem.TimedCountDuration,
                    GroupItem.ErrorGroup.Group.GroupId);
                if (groups != null && groups.ErrorGroupStats != null && groups.ErrorGroupStats.Count > 0)
                {
                    GroupItem = new ErrorGroupItem(groups.ErrorGroupStats.FirstOrDefault());
                }
                else
                {
                    GroupItem.SetEmptyModel();
                }
            }
            catch (DataSourceException ex)
            {
                ExceptionString = ex.ToString();
                ShowException = true;
            }
            finally
            {
                IsGroupLoading = false;
            }

            RaiseAllPropertyChanged();
        }

        private async Task UpdateGroupAndEventAsync()
        {
            IsControlEnabled = false;
            try
            {
                await UpdateEventGroupAsync();
                await UpdateEventAsync();
            }
            finally
            {
                IsControlEnabled = true;
            }
        }

        /// <summary>
        /// Update the event list.
        /// </summary>
        private async Task UpdateEventAsync()
        {
            EventItemCollection = null;
            RaisePropertyChanged(nameof(EventItemCollection));

            if (GroupItem.ErrorGroup.TimedCounts != null)
            {
                IsEventLoading = true;
                IsControlEnabled = false;
                ShowException = false;
                try
                {
                    var events = await _datasource.Value?.GetPageOfEventsAsync(
                        GroupItem.ErrorGroup, 
                        SelectedTimeRangeItem.EventTimeRange);
                    if (events != null && events.ErrorEvents != null)
                    {
                        EventItemCollection = CollectionViewSource.GetDefaultView(
                            events.ErrorEvents.Select(x => new EventItem(x))) as CollectionView;
                    }
                }
                catch (DataSourceException ex)
                {
                    ExceptionString = ex.ToString();
                    ShowException = true;
                }
                finally
                {
                    IsEventLoading = false;
                    IsControlEnabled = true;
                }
            }

            RaisePropertyChanged(nameof(EventItemCollection));
        }

        private StackdriverErrorReportingDataSource CreateDataSource()
        {
            if (String.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentProjectId))
            {
                return null;
            }
            return new StackdriverErrorReportingDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
        }
    }
}
