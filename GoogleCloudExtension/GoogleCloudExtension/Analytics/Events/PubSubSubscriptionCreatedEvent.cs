namespace GoogleCloudExtension.Analytics.Events
{
    internal static class PubSubSubscriptionCreatedEvent
    {
        private const string PubSubSubscriptionCreatedEventName = "pubsubSubscriptionCreated";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                PubSubSubscriptionCreatedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }

    }
}
