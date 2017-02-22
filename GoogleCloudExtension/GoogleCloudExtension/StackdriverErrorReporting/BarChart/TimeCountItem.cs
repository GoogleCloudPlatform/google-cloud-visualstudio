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
using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
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
}
