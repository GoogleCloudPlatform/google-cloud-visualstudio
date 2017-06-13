﻿using System;

namespace GoogleCloudExtension.Analytics.Events
{
    class GcsFileBrowserNewFolderEvent
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
