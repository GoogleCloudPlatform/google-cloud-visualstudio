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
using GoogleCloudExtension.DataSources.ErrorReporting;
using TimeRangeEnum=Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using EventTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Linq;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    public class TimeRangeButtonsViewModel : ViewModelBase
    {
        private TimeRangeItem _selectedTimeRangeItem;
        public TimeRangeItem SelectedTimeRangeItem
        {
            get { return _selectedTimeRangeItem; }
            set
            {
                if (_selectedTimeRangeItem != value)
                {
                    _selectedTimeRangeItem.IsCurrentSelection = false;
                    _selectedTimeRangeItem = value;
                    _selectedTimeRangeItem.IsCurrentSelection = true;
                }
            }
        }

        public List<TimeRangeItem> TimeRanges { get; }

        public event EventHandler OnTimeRangeChanged;

        public TimeRangeButtonsViewModel()
        {
            TimeRanges = new List<TimeRangeItem>();
            TimeRanges.Add(new TimeRangeItem(OnTimeRangeCommand, "1 hour", $"{60*60/30}s", TimeRangeEnum.PERIOD1HOUR, EventTimeRangeEnum.PERIOD1HOUR));
            TimeRanges.Add(new TimeRangeItem(OnTimeRangeCommand, "6 hours", $"{6*60*60/30}s", TimeRangeEnum.PERIOD6HOURS, EventTimeRangeEnum.PERIOD6HOURS));
            TimeRanges.Add(new TimeRangeItem(OnTimeRangeCommand, "1 day", $"{24*60*60/30}s", TimeRangeEnum.PERIOD1DAY, EventTimeRangeEnum.PERIOD1DAY));
            TimeRanges.Add(new TimeRangeItem(OnTimeRangeCommand, "7 days", $"{7*24*60*60/30}s", TimeRangeEnum.PERIOD1WEEK, EventTimeRangeEnum.PERIOD1WEEK));
            TimeRanges.Add(new TimeRangeItem(OnTimeRangeCommand, "30 days", $"{24*60*60}s", TimeRangeEnum.PERIOD30DAYS, EventTimeRangeEnum.PERIOD30DAYS));
            _selectedTimeRangeItem = TimeRanges.Last();
            _selectedTimeRangeItem.IsCurrentSelection = true;
        }

        public void OnTimeRangeCommand(TimeRangeItem timeRangeItem)
        {
            if (SelectedTimeRangeItem != timeRangeItem)
            {
                SelectedTimeRangeItem = timeRangeItem;
                OnTimeRangeChanged?.Invoke(this, new EventArgs());
            }
        }
    }
}
