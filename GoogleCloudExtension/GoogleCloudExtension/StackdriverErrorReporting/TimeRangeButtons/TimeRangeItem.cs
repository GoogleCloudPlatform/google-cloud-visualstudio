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

using GoogleCloudExtension.Utils;
using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using EventTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Represent the data of the error reporting time range selection.
    /// </summary>
    public class TimeRangeItem : Model
    {
        private bool _isSelected;

        /// <summary>
        /// Indicates if the item is the currently selected
        /// </summary>
        public bool IsCurrentSelection
        {
            get { return _isSelected; }
            set { SetValueAndRaise(ref _isSelected, value); }
        }

        /// <summary>
        /// The time range as caption. Examples: 1 hour, 1 day etc.
        /// </summary>
        public string Caption { get; }

        /// <summary>
        /// Gets the time range of type <seealso cref="EventGroupTimeRangeEnum"/>.
        /// </summary>
        public EventGroupTimeRangeEnum GroupTimeRange { get; }

        /// <summary>
        /// Gets the time range of type <seealso cref="EventTimeRangeEnum"/>.
        /// </summary>
        public EventTimeRangeEnum EventTimeRange { get; }

        /// <summary>
        /// String representation of time range duration.
        /// </summary>
        public string TimedCountDuration { get; }

        /// <summary>
        /// Initializes a instance of <seealso cref="TimeRangeItem"/> class.
        /// </summary>
        public TimeRangeItem(
            string caption, 
            string timedCountDuration, 
            EventGroupTimeRangeEnum timeRange, 
            EventTimeRangeEnum eventTimeRange)
        {
            Caption = caption;
            TimedCountDuration = timedCountDuration;
            EventTimeRange = eventTimeRange;
            GroupTimeRange = timeRange;
        }
    }
}
