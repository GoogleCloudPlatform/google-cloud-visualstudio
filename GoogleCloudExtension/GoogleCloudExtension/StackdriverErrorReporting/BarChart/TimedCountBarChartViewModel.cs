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
using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using GoogleCloudExtension.Accounts;
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
using System.Globalization;

namespace GoogleCloudExtension.StackdriverErrorReporting
{

    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double v0 = (double)value;
            int v1;
            int.TryParse(parameter as string, out v1);
            var ret = (int)(v0 * v1);
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TimedCountItem : Model
    {
        private readonly TimedCount _timedCount;

        private long Count => _timedCount.Count.GetValueOrDefault();

        public bool ShowTimeline => TimeLine != null;

        public string TimeLine { get; }

        public string ToolTipMessage => $"{Count} times in {"1 day"} {Environment.NewLine} Starting from {_timedCount.StartTime}.";

        public int BarHeight { get; }

        public double BarHeightRatio { get; }

        public TimedCountItem(TimedCount timedCount, string timeLine, double heightMultiplier, double countScaleMultiplier)
        {
            _timedCount = timedCount;
            TimeLine = timeLine;
            BarHeight = (int)(Count * heightMultiplier);
            BarHeightRatio = Count * countScaleMultiplier;
        }
    }

    public class XLine : Model
    {
        public string CountScale { get; }

        public int RowHeight => TimedCountBarChartViewModel.RowHeight;

        public XLine(double scale)
        {
            CountScale = scale == 0 ? null :
                String.Format(((Math.Round(scale) == scale) ? "{0:0}" : "{0:0.00}"), scale);
        }
    }


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

        public IList<TimedCountItem> TimedCountCollection { get; }

        public IList<XLine> XLines { get; }

        public TimedCountBarChartViewModel(IList<TimedCount> timedCounts, EventGroupTimeRangeEnum timeRangeEnum)
        {
            long maxCount = timedCounts == null ? 0 : MaxCountScale(timedCounts.Max(x => x.Count.GetValueOrDefault()));

            XLines = new List<XLine>();
            double countScaleUnit = (double)maxCount / RowNumber;
            for (int i = RowNumber; i > 0; --i)
            {
                XLines.Add(new XLine(countScaleUnit * i));
            }

            if (timedCounts == null)
            {
                return;
            }

            IsEmpty = false;

            heightMultiplier = BarMaxHeight / maxCount;
            countScaleMultiplier = 1.00 / maxCount;


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

            TimedCountCollection = new List<TimedCountItem>();
            int k = 0;
            Debug.Assert(timedCounts.Count > 25);
            foreach (var counter in timedCounts)
            {
                bool isVisible = (k == 0 || k == timedCounts.Count - 3 || k == timedCounts.Count / 3 || k == timedCounts.Count * 2 / 3);

                DateTime startTime = (DateTime)counter.StartTime;
                string timeLine = isVisible ? startTime.ToString(timeLineFormat) : null;

                TimedCountCollection.Add(new TimedCountItem(counter, timeLine, heightMultiplier, countScaleMultiplier));
                ++k;
            }


        }

        public TimedCountBarChartViewModel(): this(GenerateFakeRanges(), EventGroupTimeRangeEnum.PERIOD6HOURS)
        {
        }

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
