namespace GoogleCloudExtension.Analytics.Events
{
    internal static class PubSubTopicCreatedEvent
    {
        private const string PubSubTopicCreatedEventName = "pubsubTopicCreated";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                PubSubTopicCreatedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
