namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerShowAdvancedFilterHelpEvent
    {
        private const string LogsViewerShowAdvancedFilterHelpEventName = "logsViewerShowAdvancedFilterHelp";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerShowAdvancedFilterHelpEventName);
        }
    }
}
