﻿using System;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class ErrorsViewerErrorsLoadedEvent
    {
        private const string ErrorsViewerErrorsLoadedEventName = "errorsViewerErrorsLoaded";
        private const string DeploymentDurationProperty = "duration";

        public static AnalyticsEvent Create(CommandStatus status, TimeSpan duration = default(TimeSpan))
        {
            return new AnalyticsEvent(
                ErrorsViewerErrorsLoadedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status),
                DeploymentDurationProperty, duration.TotalSeconds.ToString());
        }
    }
}
