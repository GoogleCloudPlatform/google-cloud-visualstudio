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

using Google.Apis.Clouderrorreporting.v1beta1;
using Google.Apis.Clouderrorreporting.v1beta1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private TimeRangeItem _selectedTimeRange;

        /// <summary>
        /// Gets an exception as string.
        /// This is for some known possible errors. 
        /// Mostly is for DataSourceException.
        /// </summary>
        public string ExceptionString
        {
            get { return _exceptionString; }
            set { SetValueAndRaise(ref _exceptionString, value); }
        }

        /// <summary>
        /// Indicates if an exception is displayed in the window.
        /// </summary>
        public bool ShowException
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
        /// Sets the currently selected time range.
        /// </summary>
        public TimeRangeItem SelectedTimeRangeItem
        {
            set { SetValueAndRaise(ref _selectedTimeRange, value); }
        }

        /// <summary>
        /// Gets the <seealso cref="ListCollectionView"/> that contains a list of <seealso cref="ErrorGroupItem"/>.
        /// </summary>
        public ListCollectionView GroupStatsView { get; }

        /// <summary>
        /// Selected time range caption.
        /// </summary>
        public string CurrentTimeRangeCaption => String.Format(
            Resources.ErrorReportingCurrentGroupTimePeriodLabelFormat, _selectedTimeRange?.Caption);

        /// <summary>
        /// Create a new instance of <seealso cref="ErrorReportingViewModel"/> class.
        /// </summary>
        public ErrorReportingViewModel()
        {
            _dataSource = new Lazy<StackdriverErrorReportingDataSource>(CreateDataSource);
            _groupStatsCollection = new ObservableCollection<ErrorGroupItem>();
            GroupStatsView = new ListCollectionView(_groupStatsCollection);
            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// Responds to current project id change event.
        /// </summary>
        public void OnProjectIdChanged()
        {
            RaisePropertyChanged(nameof(IsGridVisible));
            _dataSource = new Lazy<StackdriverErrorReportingDataSource>(CreateDataSource);
            Reload();
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

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedTimeRangeItem):
                    if (_selectedTimeRange != null)
                    {
                        Reload();
                        RaisePropertyChanged(nameof(CurrentTimeRangeCaption));
                    }
                    break;
            }
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
        /// It shows a progress bar when waiting for data.
        /// In the end, if there is know type of exception, show the exception.
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
            ShowException = false;
            IsRefreshing = _nextPageToken == null;
            IsLoadingNextPage = _nextPageToken != null;
            try
            {
                if (_selectedTimeRange == null)
                {
                    throw new ErrorReportingException(new InvalidOperationException(nameof(_selectedTimeRange)));
                }
                results = await _dataSource.Value?.GetPageOfGroupStatusAsync(
                    _selectedTimeRange.GroupTimeRange,
                    _selectedTimeRange.TimedCountDuration,
                    nextPageToken: _nextPageToken);
            }
            catch (Exception ex) when (ex is DataSourceException || ex is ErrorReportingException)
            {
                ShowException = true;
                ExceptionString = ex.ToString();
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
                _groupStatsCollection.Add(new ErrorGroupItem(item));
            }
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
