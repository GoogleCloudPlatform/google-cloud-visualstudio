namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when the users stops a GCE VM.
    /// </summary>
    internal static class StopGceInstanceEvent
    {
        private const string StopGceInstanceEventName = "stopGceInstance";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                StopGceInstanceEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
