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
    [TemplatePart(Name = "PART_TimedCountItemsControl", Type = typeof(ItemsControl))]
    [TemplatePart(Name = "PART_LineItemsControl", Type = typeof(ItemsControl))]    
    public class TimedCountBarChartControl : Control
    {
        private ItemsControl _timedCountItemsControl;
        private ItemsControl _lineItemsControl;
        private double heightMultiplier;
        private double countScaleMultiplier;

        public const int RowNumber = 4;
        public const double BarMaxHeight = 120.00;
        public static int RowHeight => (int)(BarMaxHeight / RowNumber);

        public static readonly DependencyProperty TimedCountListProperty =
            DependencyProperty.Register(
                "TimedCountList",
                typeof(IList<TimedCount>),
                typeof(TimedCountBarChartControl),
                new FrameworkPropertyMetadata(null, OnDataChange, null));

        public static readonly DependencyProperty GroupTimeRangeProperty =
            DependencyProperty.Register(
                "GroupTimeRange",
                typeof(EventGroupTimeRangeEnum),
                typeof(TimedCountBarChartControl),
                new FrameworkPropertyMetadata(EventGroupTimeRangeEnum.PERIODUNSPECIFIED, OnDataChange, null));

        public static readonly DependencyProperty IsEmptyProperty =
            DependencyProperty.Register(
                "IsEmpty",
                typeof(bool),
                typeof(TimedCountBarChartControl),
                new FrameworkPropertyMetadata(true));

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

        /// <summary>
        /// Gets or sets the flag that indicate if the <seealso cref="TimedCountList"/> is empty.
        /// <seealso cref="IsEmptyProperty"/>.
        /// </summary>
        public bool IsEmpty
        {
            get { return (bool)GetValue(IsEmptyProperty); }
            set { SetValue(IsEmptyProperty, value); }
        }

        /// <summary>
        /// Initializes control and parts.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _timedCountItemsControl = Template.FindName("PART_TimedCountItemsControl", this) as ItemsControl;
            _lineItemsControl = Template.FindName("PART_LineItemsControl", this) as ItemsControl;
        }

        private static void OnDataChange(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            TimedCountBarChartControl control = source as TimedCountBarChartControl;
            if (e.NewValue != e.OldValue)
            {
                control.OnUpdateTimedCountItems();
            }
        }

        private void OnUpdateTimedCountItems()
        {
            if (TimedCountList == null)
            {
                IsEmpty = true;
                return;
            }

            IsEmpty = false;

            long maxCount = TimedCountList == null ? 0 : MaxCountScale(TimedCountList.Max(x => x.Count.GetValueOrDefault()));

            if (_lineItemsControl != null)
            {
                CreateLineItems(maxCount);
            }

            if (_timedCountItemsControl != null)
            {
                CreateTimedCountItems(maxCount);
            }
        }

        private void CreateTimedCountItems(long maxCount)
        {
            heightMultiplier = BarMaxHeight / maxCount;
            countScaleMultiplier = 1.00 / maxCount;

            string timeLineFormat = GroupTimeRange.TimeLineFormat();
            var timedCountItemList = new List<TimedCountItem>();
            int k = 0;
            foreach (var counter in TimedCountList)
            {
                // time line is shown first, last-3, and optional 2 in the middle.
                bool isTimeLineVisible = (k == 0 || k == TimedCountList.Count - 3 || k == TimedCountList.Count / 3 || k == TimedCountList.Count * 2 / 3);
                DateTime startTime = (DateTime)counter.StartTime;
                string timeLine = isTimeLineVisible ? startTime.ToString(timeLineFormat) : null;

                timedCountItemList.Add(new TimedCountItem(counter, timeLine, heightMultiplier, countScaleMultiplier, GroupTimeRange.TimeCountDuration()));
                ++k;
            }

            _timedCountItemsControl.ItemsSource = timedCountItemList;
        }

        private void CreateLineItems(long maxCount)
        {
            var lineItems = new List<XLineItem>();
            double countScaleUnit = (double)maxCount / RowNumber;
            for (int i = RowNumber; i > 0; --i)
            {
                lineItems.Add(new XLineItem(countScaleUnit * i));
            }

            _lineItemsControl.ItemsSource = lineItems;
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
