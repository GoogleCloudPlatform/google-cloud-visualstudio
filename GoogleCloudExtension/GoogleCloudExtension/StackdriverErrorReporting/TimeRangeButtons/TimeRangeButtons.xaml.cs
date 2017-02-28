﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using TimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using EventTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Interaction logic for TimeRangeButtons.xaml
    /// </summary>
    public partial class TimeRangeButtons : ItemsControl
    {
        /// <summary>
        /// The list of <seealso cref="TimeRangeItem"/> as the data source of time range buttons.
        /// </summary>
        private readonly List<TimeRangeItem> TimeRangeItems =
            new List<TimeRangeItem>(
                new TimeRangeItem[]
                {
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting1HourButtonCaption, 
                        $"{60 * 60 / 30}s",
                        TimeRangeEnum.PERIOD1HOUR,
                        EventTimeRangeEnum.PERIOD1HOUR),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting6HoursButtonCaption,
                        $"{6 * 60 * 60 / 30}s",
                        TimeRangeEnum.PERIOD6HOURS,
                        EventTimeRangeEnum.PERIOD6HOURS),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting1DayButtonCaption,
                        $"{24 * 60 * 60 / 30}s",
                        TimeRangeEnum.PERIOD1DAY,
                        EventTimeRangeEnum.PERIOD1DAY),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting7DaysButtonCaption,
                        $"{7 * 24 * 60 * 60 / 30}s",
                        TimeRangeEnum.PERIOD1WEEK,
                        EventTimeRangeEnum.PERIOD1WEEK),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting30DaysButtonCaption,
                        $"{24 * 60 * 60}s",
                        TimeRangeEnum.PERIOD30DAYS,
                        EventTimeRangeEnum.PERIOD30DAYS)
                });

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(TimeRangeItem),
                typeof(TimeRangeButtons),
                new FrameworkPropertyMetadata(null, OnTimePartPropertyChanged, null));

        public static readonly DependencyProperty OnItemSelectedCommandProperty =
            DependencyProperty.Register(
                nameof(OnItemSelectedCommand),
                typeof(ICommand),
                typeof(TimeRangeButtons),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The selected <seealso cref="TimeRangeItem"/> .
        /// </summary>
        public TimeRangeItem SelectedItem
        {
            get { return (TimeRangeItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        /// <summary>
        /// The command that respond to the range item button click event.
        /// </summary>
        public ICommand OnItemSelectedCommand
        {
            get { return (ICommand)GetValue(OnItemSelectedCommandProperty); }
            set { SetValue(OnItemSelectedCommandProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of <seealso cref="TimeRangeButtons"/> class;
        /// </summary>
        public TimeRangeButtons()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Override the <seealso cref="OnApplyTemplate"/> method to initialize controls.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SelectedItem = TimeRangeItems.Last();
            ItemsSource = TimeRangeItems;
            OnItemSelectedCommand = new ProtectedCommand<TimeRangeItem>((item) =>
            {
                SelectedItem = item;    
            });
        }

        private static void OnTimePartPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var newValue = e.NewValue as TimeRangeItem;
            var oldValue = e.OldValue as TimeRangeItem;
            if (oldValue != null)
            {
                oldValue.IsCurrentSelection = false;
            }
            if (newValue != null)
            {
                newValue.IsCurrentSelection = true;
            }
        }
    }
}
