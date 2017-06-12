﻿namespace GoogleCloudExtension.Analytics.Events
{
    internal static class PubSubTopicDeletedEvent
    {
        private const string PubSubTopicDeletedEventName = "pubsubTopicDeleted";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                PubSubTopicDeletedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
