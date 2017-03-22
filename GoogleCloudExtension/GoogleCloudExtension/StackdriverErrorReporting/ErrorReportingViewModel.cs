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

using Google.Apis.Clouderrorreporting.v1beta1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// The view model for <seealso cref="ErrorReportingToolWindowControl"/>.
    /// </summary>
    public class ErrorReportingViewModel : ViewModelBase
    {
        private Lazy<StackdriverErrorReportingDataSource> _dataSource;

        private string _nextPageToken;
        private bool _isLoading;
        private bool _isRefreshing;
        private bool _isLoadingNextPage;
        private bool _showException;
        private string _exceptionString;
        private ObservableCollection<ErrorGroupItem> _groupStatsCollection;
        private Lazy<List<TimeRangeItem>> _timeRangeItemList = new Lazy<List<TimeRangeItem>>(TimeRangeItem.CreateTimeRanges);
        private TimeRangeItem _selectedTimeRange;

        /// <summary>
        /// Gets an exception as string.
        /// This is for some known possible errors. 
        /// Mostly is for DataSourceException.
        /// </summary>
        public string ErrorString
        {
            get { return _exceptionString; }
            set { SetValueAndRaise(ref _exceptionString, value); }
        }

        /// <summary>
        /// Indicates if an exception is displayed in the window.
        /// </summary>
        public bool ShowError
        {
            get { return _showException; }
            set { SetValueAndRaise(ref _showException, value); }
        }

        /// <summary>
        /// Indicates if loading data is complete.
        /// </summary>
        public bool IsLoadingComplete
        {
            get { return !_isLoading; }
            set { SetValueAndRaise(ref _isLoading, !value); }
        }

        /// <summary>
        /// Indicates if the view refresh button is pressed.
        /// </summary>
        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set { SetValueAndRaise(ref _isRefreshing, value); }
        }

        /// <summary>
        /// Indicates if it is loading next page of data.
        /// </summary>
        public bool IsLoadingNextPage
        {
            get { return _isLoadingNextPage; }
            set { SetValueAndRaise(ref _isLoadingNextPage, value); }
        }

        /// <summary>
        /// If the current project id is reset to null or empty, hide the grid. 
        /// </summary>
        public bool IsGridVisible => !String.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentProjectId);

        /// <summary>
        /// Gets the list of <seealso cref="TimeRangeItem"/> as data source for time range selector.
        /// </summary>
        public List<TimeRangeItem> TimeRangeItemList => _timeRangeItemList.Value;

        /// <summary>
        /// Sets the currently selected time range.
        /// </summary>
        public TimeRangeItem SelectedTimeRangeItem
        {
            get { return _selectedTimeRange; }
            set
            {
                SetValueAndRaise(ref _selectedTimeRange, value);
                if (_selectedTimeRange != null)
                {
                    Reload();
                    RaisePropertyChanged(nameof(CurrentTimeRangeCaption));
                }
            }
        }

        /// <summary>
        /// Gets the <seealso cref="ListCollectionView"/> that contains a list of <seealso cref="ErrorGroupItem"/>.
        /// </summary>
        public ListCollectionView GroupStatsView { get; }

        /// <summary>
        /// Navigate to detail view window command.
        /// </summary>
        public ProtectedCommand<ErrorGroupItem> OnGotoDetailCommand { get; }

        /// <summary>
        /// Responds to auto reload command.
        /// </summary>
        public ProtectedCommand OnAutoReloadCommand { get; }

        /// <summary>
        /// Selected time range caption.
        /// </summary>
        public string CurrentTimeRangeCaption => String.Format(
            Resources.ErrorReportingCurrentGroupTimePeriodLabelFormat, SelectedTimeRangeItem?.Caption);

        /// <summary>
        /// Create a new instance of <seealso cref="ErrorReportingViewModel"/> class.
        /// </summary>
        public ErrorReportingViewModel()
        {
            _dataSource = new Lazy<StackdriverErrorReportingDataSource>(CreateDataSource);
            _groupStatsCollection = new ObservableCollection<ErrorGroupItem>();
            GroupStatsView = new ListCollectionView(_groupStatsCollection);
            SelectedTimeRangeItem = TimeRangeItemList.Last();
            OnGotoDetailCommand = new ProtectedCommand<ErrorGroupItem>(NavigateToDetailWindow);
            OnAutoReloadCommand = new ProtectedCommand(Reload);
            CredentialsStore.Default.CurrentProjectIdChanged += (sender, e) => OnProjectIdChanged();
            CredentialsStore.Default.Reset += (sender, e) => OnProjectIdChanged();
        }

        /// <summary>
        /// Load next page of error groups.
        /// </summary>
        public void LoadNextPage()
        {
            if (_nextPageToken == null)
            {
                Debug.WriteLine("_nextPageToken is null, there is no more events group to load");
                return;
            }

            LoadAsync();
        }

        /// <summary>
        /// Responds to current project id change event.
        /// </summary>
        private void OnProjectIdChanged()
        {
            RaisePropertyChanged(nameof(IsGridVisible));
            _dataSource = new Lazy<StackdriverErrorReportingDataSource>(CreateDataSource);
            Reload();
        }

        /// <summary>
        /// Reload first page of <seealso cref="ErrorGroupStats"/>.
        /// </summary>
        private void Reload()
        {
            _groupStatsCollection.Clear();
            _nextPageToken = null;
            LoadAsync();
        }

        /// <summary>
        /// Load data from Google Cloud Error Reporting API service end point.
        /// It shows a progress control when waiting for data.
        /// In the end, if there is known type of exception, show a generic error..
        /// </summary>
        private async Task LoadAsync()
        {
            if (_isLoading)
            {
                Debug.WriteLine("_isLoading is true, quit LoadAsync.");
                return;
            }

            IsLoadingComplete = false;
            ListGroupStatsResponse results = null;
            ShowError = false;
            IsRefreshing = _nextPageToken == null;
            IsLoadingNextPage = _nextPageToken != null;
            try
            {
                if (SelectedTimeRangeItem == null)
                {
                    throw new ErrorReportingException(new InvalidOperationException(nameof(SelectedTimeRangeItem)));
                }
                results = await _dataSource.Value?.GetPageOfGroupStatusAsync(
                    SelectedTimeRangeItem.GroupTimeRange,
                    SelectedTimeRangeItem.TimedCountDuration,
                    nextPageToken: _nextPageToken);
            }
            catch (DataSourceException)
            {
                ShowError = true;
                ErrorString = Resources.ErrorReportingDataSourceGenericErrorMessage;
            }
            catch (ErrorReportingException)
            {
                ShowError = true;
                ErrorString = Resources.ErrorReportingInternalCodeErrorGenericMessage;
            }
            finally
            {
                IsLoadingComplete = true;
                IsRefreshing = false;
                IsLoadingNextPage = false;
            }

            // results can be null when (1) there is exception. (2) current account is empty.
            _nextPageToken = results?.NextPageToken;
            AddItems(results?.ErrorGroupStats);
        }

        private void AddItems(IList<ErrorGroupStats> groupStats)
        {
            if (groupStats == null)
            {
                return;
            }

            Debug.WriteLine($"Gets {groupStats.Count} items");
            foreach (var item in groupStats)
            {
                if (item == null)
                {
                    return;
                }
                _groupStatsCollection.Add(new ErrorGroupItem(item, SelectedTimeRangeItem));
            }
        }

        private void NavigateToDetailWindow(ErrorGroupItem groupItem)
        {
            var window = ToolWindowCommandUtils.ShowToolWindow<ErrorReportingDetailToolWindow>();
            window.ViewModel.UpdateView(groupItem, _selectedTimeRange);
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
