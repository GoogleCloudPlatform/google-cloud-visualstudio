namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerAdvancedFilterEvent
    {
        private const string LogsViewerAdvancedFilterEventName = "logsViewerAdvancedFilter";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerAdvancedFilterEventName);
        }
    }
}
