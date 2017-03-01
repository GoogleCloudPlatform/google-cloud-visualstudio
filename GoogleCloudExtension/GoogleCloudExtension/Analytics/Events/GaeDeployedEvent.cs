using System;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent after a deployment to App Engine Flex is finished. 
    /// </summary>
    internal static class GaeDeployedEvent
    {
        private const string GaeDeployedEventName = "flexDeployment";
        private const string DeploymentDurationProperty = "duration";

        public static AnalyticsEvent Create(CommandStatus status, TimeSpan duration = default(TimeSpan))
        {
            return new AnalyticsEvent(
                GaeDeployedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status),
                DeploymentDurationProperty, duration.TotalSeconds.ToString());
        }
    }
}
