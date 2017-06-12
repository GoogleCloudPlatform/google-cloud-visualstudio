namespace GoogleCloudExtension.Analytics.Events
{
    internal static class PubSubTopicsLoadedEvent
    {
        private const string PubSubTopicsLoadedEventName = "pubsubTopicsLoaded";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                PubSubTopicsLoadedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
