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

using System;

namespace GoogleCloudExtension.Analytics.Events
{
    internal class GcsFileBrowserNewFolderEvent
    {
        private const string GcsFileGcsFileBrowserNewFolderEventName = "gcsFileBrowserNewFolder";
        private const string DeploymentDurationProperty = "duration";

        public static AnalyticsEvent Create(CommandStatus status, TimeSpan duration = default(TimeSpan))
        {
            return new AnalyticsEvent(
                GcsFileGcsFileBrowserNewFolderEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status),
                DeploymentDurationProperty, duration.TotalSeconds.ToString());
        }
    }
}
