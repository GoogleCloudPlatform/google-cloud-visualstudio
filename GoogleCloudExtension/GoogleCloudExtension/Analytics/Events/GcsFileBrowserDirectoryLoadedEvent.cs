using System;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserDirectoryLoadedEvent
    {
        private const string GcsFileBrowserDirectoryLoadedEventName = "gcsFileBrowserDirectoryLoaded";
        private const string DeploymentDurationProperty = "duration";

        public static AnalyticsEvent Create(CommandStatus status, TimeSpan duration = default(TimeSpan))
        {
            return new AnalyticsEvent(
                GcsFileBrowserDirectoryLoadedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status),
                DeploymentDurationProperty, duration.TotalSeconds.ToString());
        }
    }
}
