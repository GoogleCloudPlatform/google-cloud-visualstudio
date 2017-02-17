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
using GoogleCloudExtension.Utils;
using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using EventTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Represent the data to the error reporting time range selection.
    /// </summary>
    public class TimeRangeItem : Model
    {
        private bool _isSelected;
        public bool IsCurrentSelection
        {
            get { return _isSelected; }
            set { SetValueAndRaise(ref _isSelected, value); }
        }

        public ProtectedCommand TimeRangeCommand { get; }

        public string Caption { get; }

        public EventGroupTimeRangeEnum TimeRange { get; }
        public EventTimeRangeEnum EventTimeRange { get; }

        public string TimedCountDuration { get; }

        public TimeRangeItem(Action<TimeRangeItem> commandActionCallback, string caption, string timedCountDuration, EventGroupTimeRangeEnum timeRange, EventTimeRangeEnum eventTimeRange)
        {
            TimeRangeCommand = new ProtectedCommand(() => commandActionCallback(this));
            Caption = caption;
            TimedCountDuration = timedCountDuration;
            EventTimeRange = eventTimeRange;
            TimeRange = timeRange;
        }
    }
}
