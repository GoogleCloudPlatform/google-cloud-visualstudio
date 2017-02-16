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

using Google.Apis.Clouderrorreporting.v1beta1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.ErrorReporting;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Linq;
using System.Windows.Threading;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// The view model for Stackdriver Error Reporting overview Window
    /// </summary>
    class ErrorReportingViewModel : ViewModelBase
    {
        static ErrorReportingViewModel()
        {
            Instance = new ErrorReportingViewModel();
        }

        private string _nextPageToken;
        private bool _isLoading;
        private bool _isRefreshing;
        private bool _isLoadingNextPage;

        private bool _showException;
        private string _exceptionString;

        public string ExceptionString
        {
            get { return _exceptionString; }
            set { SetValueAndRaise(ref _exceptionString, value); }
        }

        public bool ShowException
        {
            get { return _showException; }
            set { SetValueAndRaise(ref _showException, value); }
        }

        public bool IsLoadingComplete
        {
            get { return !_isLoading; }
            set { SetValueAndRaise(ref _isLoading, !value); }
        }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set { SetValueAndRaise(ref _isRefreshing, value); }
        }

        public bool IsLoadingNextPage
        {
            get { return _isLoadingNextPage; }
            set { SetValueAndRaise(ref _isLoadingNextPage, value); }
        }

        public static ErrorReportingViewModel Instance { get; }

        public TimeRangeButtonsViewModel TimeRangeButtonsModel { get; }

        private ObservableCollection<ErrorGroupItem> _groupStatsCollection;

        public ListCollectionView GroupStatsView { get; }

        public string CurrentTimeRangeCaption => TimeRangeButtonsModel.SelectedTimeRangeItem.Caption;

        public ErrorReportingViewModel()
        {
            TimeRangeButtonsModel = new TimeRangeButtonsViewModel();
            TimeRangeButtonsModel.OnTimeRangeChanged += OnTimeRangeChanged;
            _groupStatsCollection = new ObservableCollection<ErrorGroupItem>();
            GroupStatsView = new ListCollectionView(_groupStatsCollection);
            Refresh();
        }

        private void OnTimeRangeChanged(object sender, EventArgs v)
        {
            Refresh();
            RaisePropertyChanged(nameof(CurrentTimeRangeCaption));
        }

        public void Refresh()
        {
            _groupStatsCollection.Clear();
            LoadAsync();
        }

        public void LoadNextPage()
        {
            if (_isLoading)
            {
                Debug.WriteLine("isLoading is true, skip LoadNextPage");
                return;
            }

            if (_nextPageToken == null)
            {
                Debug.WriteLine("_nextPageToken is null, there is no more events group to load");
                return;
            }

            LoadAsync(refresh: false);
        }

        private async Task LoadAsync(bool refresh = true)
        {
            IsLoadingComplete = false;
            GroupStatsRequestResult results = null;
            ShowException = false;
            _nextPageToken = null;
            if (refresh)
            {
                IsRefreshing = true;
            }
            else
            {
                IsLoadingNextPage = true;
            }
            try
            {
                results = await SerDataSourceInstance.Instance.Value?.ListGroupStatusAsync(
                    TimeRangeButtonsModel.SelectedTimeRangeItem.TimeRange,
                    TimeRangeButtonsModel.SelectedTimeRangeItem.TimedCountDuration,
                    nextPageToken: _nextPageToken);
            }
            catch (DataSourceException ex)
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
            AddItems(results?.GroupStats);
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

                _groupStatsCollection.Add(new ErrorGroupItem(item, TimeRangeButtonsModel.SelectedTimeRangeItem.TimeRange));
            }
        }
    }
}
