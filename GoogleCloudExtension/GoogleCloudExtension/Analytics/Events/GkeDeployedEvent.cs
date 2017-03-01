using System;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent after a deployment to GKE.
    /// </summary>
    internal static class GkeDeployedEvent
    {
        private const string GkeDeployedEventName = "gkeDeployment";
        private const string DeploymentDurationProperty = "duration";

        public static AnalyticsEvent Create(CommandStatus status, TimeSpan duration = default(TimeSpan))
        {
            return new AnalyticsEvent(
                GkeDeployedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status),
                DeploymentDurationProperty, duration.TotalSeconds.ToString());
        }
    }
}
