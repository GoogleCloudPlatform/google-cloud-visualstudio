namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerSimpleTextSearchEvent
    {
        private const string LogsViewerSimpleTextSearchEventName = "logsViewersimpleTextSearch";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerSimpleTextSearchEventName);
        }
    }
}
