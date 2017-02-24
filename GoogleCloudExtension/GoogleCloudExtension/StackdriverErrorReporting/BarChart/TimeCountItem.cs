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

using Google.Apis.Clouderrorreporting.v1beta1.Data;
using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Wrapper on top of <seealso cref="TimedCount"/> as the bar chart item.
    /// </summary>
    public class TimedCountItem : Model
    {
        private readonly TimedCount _timedCount;
        private long Count => _timedCount.Count.GetValueOrDefault();

        /// <summary>
        /// Gets the flag that indicates if time line should be shown.
        /// </summary>
        public bool ShowTimeline => TimeLine != null;

        /// <summary>
        /// Gets time line string.
        /// </summary>
        public string TimeLine { get; }

        /// <summary>
        /// Gets tooltip text.
        /// </summary>
        public string ToolTipMessage { get; }

        /// <summary>
        /// Gets the bar height.
        /// </summary>
        public int BarHeight { get; }

        /// <summary>
        /// Gets the ratio of bar height per error count.
        /// </summary>
        public double BarHeightRatio { get; }

        /// <summary>
        /// Initializes a new instance of <seealso cref="TimedCountItem"/> class.
        /// </summary>
        public TimedCountItem(
            TimedCount timedCount, 
            string timeLine, 
            double heightMultiplier, 
            double countScaleMultiplier, 
            string timeRangeLabel)
        {
            _timedCount = timedCount;
            // Example, $"{Count} times in {"1 day"} {Environment.NewLine} Starting from {_timedCount.StartTime}.";
            ToolTipMessage = String.Format(Resources.ErrorReportingBarchartTooltipFormat, 
                 Count, timeRangeLabel, Environment.NewLine, _timedCount.StartTime);
            TimeLine = timeLine;
            BarHeight = (int)(Count * heightMultiplier);
            BarHeightRatio = Count * countScaleMultiplier;
        }
    }
}
