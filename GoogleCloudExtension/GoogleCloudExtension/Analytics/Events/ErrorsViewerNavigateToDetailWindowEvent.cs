namespace GoogleCloudExtension.Analytics.Events
{
    internal static class ErrorsViewerNavigateToDetailWindowEvent
    {
        private const string ErrorsViewerNavigateToDetailWindowEventName = "errorsViewerNavigateToDetailWindow";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(ErrorsViewerNavigateToDetailWindowEventName);
        }
    }
}
