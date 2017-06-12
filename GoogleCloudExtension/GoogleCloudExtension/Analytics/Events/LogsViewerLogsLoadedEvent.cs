using System;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerLogsLoadedEvent
    {
        private const string LogsViewerLogsLoadedEventName = "logsViewerLogsLoaded";
        private const string DeploymentDurationProperty = "duration";

        public static AnalyticsEvent Create(CommandStatus status, TimeSpan duration = default(TimeSpan))
        {
            return new AnalyticsEvent(
                LogsViewerLogsLoadedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status),
                DeploymentDurationProperty, duration.TotalSeconds.ToString());
        }
    }
}
