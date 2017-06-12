namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerCancelRequestEvent
    {
        private const string LogsViewerCancelRequestEventName = "logsViewerCancelRequest";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerCancelRequestEventName);
        }
    }
}
