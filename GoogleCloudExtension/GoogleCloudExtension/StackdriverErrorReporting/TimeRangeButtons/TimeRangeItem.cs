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
using System.Collections.Generic;
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

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (!(obj is TimeRangeItem))
            {
                return false;
            }
            else
            {
                var other = (TimeRangeItem)obj;
                return Caption.Equals(other.Caption) && TimedCountDuration.Equals(other.TimedCountDuration) &&
                    EventTimeRange.Equals(other.EventTimeRange) && GroupTimeRange.Equals(other.GroupTimeRange);
            }
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hashCode = Caption.GetHashCode();
            hashCode *= 31;
            hashCode += TimedCountDuration.GetHashCode();
            hashCode *= 31;
            hashCode += EventTimeRange.GetHashCode();
            hashCode *= 31;
            hashCode += GroupTimeRange.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Create a list of <seealso cref="TimeRangeItem"/> for Error Reporting windows.
        /// Create new list instead of using a static instance, 
        /// because overview window, and detail window each need an independent instance of the list.
        /// </summary>
        public static List<TimeRangeItem> CreateTimeRanges()
        {
            return new List<TimeRangeItem>(
                new TimeRangeItem[]
                {
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting1HourButtonCaption,
                        $"{60 * 60 / 30}s",
                        EventGroupTimeRangeEnum.PERIOD1HOUR,
                        EventTimeRangeEnum.PERIOD1HOUR),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting6HoursButtonCaption,
                        $"{6 * 60 * 60 / 30}s",
                        EventGroupTimeRangeEnum.PERIOD6HOURS,
                        EventTimeRangeEnum.PERIOD6HOURS),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting1DayButtonCaption,
                        $"{24 * 60 * 60 / 30}s",
                        EventGroupTimeRangeEnum.PERIOD1DAY,
                        EventTimeRangeEnum.PERIOD1DAY),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting7DaysButtonCaption,
                        $"{7 * 24 * 60 * 60 / 30}s",
                        EventGroupTimeRangeEnum.PERIOD1WEEK,
                        EventTimeRangeEnum.PERIOD1WEEK),
                    new TimeRangeItem(
                        GoogleCloudExtension.Resources.ErrorReporting30DaysButtonCaption,
                        $"{24 * 60 * 60}s",
                        EventGroupTimeRangeEnum.PERIOD30DAYS,
                        EventTimeRangeEnum.PERIOD30DAYS)
                });
        }
    }
}
