namespace GoogleCloudExtension.Analytics.Events
{
    internal static class PubSubSubscriptionDeletedEvent
    {
        private const string PubSubSubscriptionDeletedEventName = "pubsubSubscriptionDeleted";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                PubSubSubscriptionDeletedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
