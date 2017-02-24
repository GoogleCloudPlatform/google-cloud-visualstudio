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

using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using static GoogleCloudExtension.Resources;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// The class contains some extension methods used by Error Reporting.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Returns bar chart time line format string based on the <seealso cref="EventGroupTimeRangeEnum"/> value.
        /// </summary>
        public static string TimeLineFormat(this EventGroupTimeRangeEnum timeRange  )
        {
            switch (timeRange)
            {
                case EventGroupTimeRangeEnum.PERIOD1WEEK:
                case EventGroupTimeRangeEnum.PERIOD30DAYS:
                    return ErrorReportingDayTimeLineFormat;
                case EventGroupTimeRangeEnum.PERIOD1DAY:
                case EventGroupTimeRangeEnum.PERIOD1HOUR:
                case EventGroupTimeRangeEnum.PERIOD6HOURS:
                    return ErrorReportingTimeTimeLineFormat;
                default:
                    return ErrorReportingDefaultTimeLineFormat;
            }
        }

        /// <summary>
        /// Returns time count duration to a <seealso cref="EventGroupTimeRangeEnum"/> value.
        /// </summary>
        public static string TimeCountDuration(this EventGroupTimeRangeEnum timeRange)
        {
            switch (timeRange)
            {
                case EventGroupTimeRangeEnum.PERIOD1WEEK:
                    return ErrorReporting7DayRangeTimeCountDurationLabel;
                case EventGroupTimeRangeEnum.PERIOD30DAYS:
                    return ErrorReporting30DaysRangeTimeCountDurationLabel;
                case EventGroupTimeRangeEnum.PERIOD1DAY:
                    return ErrorReporting1DayRangeTimeCountDurationLabel;
                case EventGroupTimeRangeEnum.PERIOD1HOUR:
                    return ErrorReporting1HourRangeTimeCountDurationLabel;
                case EventGroupTimeRangeEnum.PERIOD6HOURS:
                    return ErrorReporting6HoursRangeTimeCountDurationLabel;
                default:
                    return "";      // Not expected, to be safe, return empty string.
            }
        }
    }
}
