namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerOpenEvent
    {
        private const string LogsViewerOpenEventName = "logsViewerOpen";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerOpenEventName);
        }
    }
}
