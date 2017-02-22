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
using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources.ErrorReporting;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Linq;
using System.Globalization;

namespace GoogleCloudExtension.StackdriverErrorReporting
{ 
    public class TimedCountBarChartViewModel : ViewModelBase
    {
        public const int RowNumber = 4;
        public const double BarMaxHeight = 120.00;
        public static int RowHeight => (int)(BarMaxHeight / RowNumber);
        private double heightMultiplier;
        private double countScaleMultiplier;

        private bool _isEmpty = true;
        public bool IsEmpty
        {
            get { return _isEmpty; }
            set { SetValueAndRaise(ref _isEmpty, value); }
        }

        public IList<TimedCountItem> TimedCountItemCollection { get; private set; }

        private IList<TimedCount> _timedCountCollection;
        public IList<TimedCount> TimedCountCollection
        {
            get { return _timedCountCollection; }
            set { SetValueAndRaise(ref _timedCountCollection, value); }
        }

        private TimedCountItem _selectedItem;
        public TimedCountItem SelectedItem
        {
            get { return _selectedItem; }
            set { SetValueAndRaise(ref _selectedItem, value); }
        }

        private EventGroupTimeRangeEnum _groupTimeRange;
        public EventGroupTimeRangeEnum GroupTimeRange
        {
            get { return _groupTimeRange; }
            set { SetValueAndRaise(ref _groupTimeRange, value); }
        }

        public IList<XLineItem> XLines { get; private set; }

        public TimedCountBarChartViewModel()
        {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TimedCountCollection):
                    OnUpdateTimedCountItems();
                    break;
            }
        }

        private void OnUpdateTimedCountItems()
        {
            var timedCounts = TimedCountCollection;
            long maxCount = _timedCountCollection == null ? 0 : MaxCountScale(timedCounts.Max(x => x.Count.GetValueOrDefault()));

            XLines = new List<XLineItem>();
            double countScaleUnit = (double)maxCount / RowNumber;
            for (int i = RowNumber; i > 0; --i)
            {
                XLines.Add(new XLineItem(countScaleUnit * i));
            }

            if (timedCounts == null)
            {
                return;
            }

            IsEmpty = false;

            heightMultiplier = BarMaxHeight / maxCount;
            countScaleMultiplier = 1.00 / maxCount;

            var timeRangeEnum = GroupTimeRange;
            string timeLineFormat;
            switch (timeRangeEnum)
            {
                case EventGroupTimeRangeEnum.PERIOD1WEEK:
                case EventGroupTimeRangeEnum.PERIOD30DAYS:
                    timeLineFormat = "MMM d";
                    break;
                case EventGroupTimeRangeEnum.PERIOD1DAY:
                case EventGroupTimeRangeEnum.PERIOD1HOUR:
                case EventGroupTimeRangeEnum.PERIOD6HOURS:
                    timeLineFormat = "hh:mm tt";
                    break;
                default:
                    timeLineFormat = "MMM d HH:mm";
                    break;
            }

            var timedCountItemList = new List<TimedCountItem>();
            int k = 0;
            Debug.Assert(timedCounts.Count > 25);
            foreach (var counter in timedCounts)
            {
                bool isVisible = (k == 0 || k == timedCounts.Count - 3 || k == timedCounts.Count / 3 || k == timedCounts.Count * 2 / 3);

                DateTime startTime = (DateTime)counter.StartTime;
                string timeLine = isVisible ? startTime.ToString(timeLineFormat) : null;

                timedCountItemList.Add(new TimedCountItem(counter, timeLine, heightMultiplier, countScaleMultiplier));
                ++k;
            }

            TimedCountItemCollection = timedCountItemList;
        }

        //public TimedCountBarChartViewModel(): this(GenerateFakeRanges(), EventGroupTimeRangeEnum.PERIOD6HOURS)
        //{
        //}

        private long MaxCountScale(long maxCount)
        {
            if (maxCount <= 0)
            {
                return 1;
            }

            return (long)(Math.Ceiling((double)maxCount/RowNumber)) * RowNumber;
        }

        private static IList<TimedCount> GenerateFakeRanges()
        {
            List<TimedCount> tCounts = new List<TimedCount>();
            for (int i = 30; i > 0; --i)
            {
                TimedCount t = new TimedCount();
                t.StartTime = DateTime.UtcNow.AddDays(-1 * i);
                t.EndTime = DateTime.UtcNow.AddDays(-1 * i + 1);
                t.Count = i == 5 ? 45 : (i%3)*i;
                if (i == 2) { t.Count = 15; }
                if (i == 7) { t.Count = 30; }
                tCounts.Add(t);
            }

            return tCounts;
        }
    }
}
