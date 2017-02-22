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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Base class for bar chart control on a collection of <seealso cref="TimedCount"/>
    /// </summary>
    [TemplatePart(Name = "PART_chartItemsControl", Type = typeof(ItemsControl))]
    public class TimedCountBarChartControl : Control
    {
        private ItemsControl _chartItemsControl;

        public static readonly DependencyProperty TimedCountListProperty =
            DependencyProperty.Register(
                "TimedCountList",
                typeof(IList<TimedCount>),
                typeof(TimedCountBarChartControl),
                new FrameworkPropertyMetadata(null, OnTimedCountListChanged, null));

        public static readonly DependencyProperty GroupTimeRangeProperty =
            DependencyProperty.Register(
                "GroupTimeRange",
                typeof(EventGroupTimeRangeEnum),
                typeof(TimedCountBarChartControl),
                new FrameworkPropertyMetadata(EventGroupTimeRangeEnum.PERIODUNSPECIFIED));

        /// <summary>
        /// Gets or sets the list of <seealso cref="TimedCount"/>.
        /// <seealso cref="TimedCountListProperty"/>.
        /// </summary>
        public IList<TimedCount> TimedCountList
        {
            get { return (IList<TimedCount>)GetValue(TimedCountListProperty); }
            set { SetValue(TimedCountListProperty, value); }
        }

        /// <summary>
        /// Gets or sets the group time range.
        /// <seealso cref="GroupTimeRangeProperty"/>.
        /// </summary>
        public EventGroupTimeRangeEnum GroupTimeRange
        {
            get { return (EventGroupTimeRangeEnum)GetValue(GroupTimeRangeProperty); }
            set { SetValue(GroupTimeRangeProperty, value); }
        }

        //static TimedCountBarChartControl()
        //{
        //    DefaultStyleKeyProperty.OverrideMetadata(typeof(TimedCountBarChartControl), new FrameworkPropertyMetadata(typeof(TimedCountBarChartControl)));
        //}

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _chartItemsControl = Template.FindName("PART_ChartItemsControl", this) as ItemsControl;
        }

        public const int RowNumber = 4;
        public const double BarMaxHeight = 120.00;
        public static int RowHeight => (int)(BarMaxHeight / RowNumber);
        private double heightMultiplier;
        private double countScaleMultiplier;

        //private bool _isEmpty = true;
        //public bool IsEmpty
        //{
        //    get { return _isEmpty; }
        //    set { SetValueAndRaise(ref _isEmpty, value); }
        //}

        public IList<TimedCountItem> TimedCountItemCollection { get; private set; }

        public IList<XLineItem> XLines { get; private set; }


        private static void OnTimedCountListChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            TimedCountBarChartControl control = source as TimedCountBarChartControl;
            IList<TimedCount> newValue = (IList<TimedCount>)e.NewValue;
            IList<TimedCount> oldValue = (IList<TimedCount>)e.OldValue;
            // SetTimeParts triggers OnTimePartPropertyChanged
            // And it sets control.Time that calls back to current method OnTimePropertyChanged.
            // This must be checked to avoid such a deadloop. 
            if (newValue != oldValue)
            {
                control.OnUpdateTimedCountItems();
                if (control._chartItemsControl != null)
                {
                    control._chartItemsControl.ItemsSource = control.TimedCountItemCollection;
                }
            }
        }

        private void OnUpdateTimedCountItems()
        {
            var timedCounts = TimedCountList;
            long maxCount = TimedCountList == null ? 0 : MaxCountScale(timedCounts.Max(x => x.Count.GetValueOrDefault()));

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

            // IsEmpty = false;

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

        private long MaxCountScale(long maxCount)
        {
            if (maxCount <= 0)
            {
                return 1;
            }

            return (long)(Math.Ceiling((double)maxCount / RowNumber)) * RowNumber;
        }
    }
}
