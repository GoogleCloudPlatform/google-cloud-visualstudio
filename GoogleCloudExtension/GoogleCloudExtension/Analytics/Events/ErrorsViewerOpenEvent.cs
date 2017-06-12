namespace GoogleCloudExtension.Analytics.Events
{
    internal static class ErrorsViewerOpenEvent
    {
        private const string ErrorsViewerOpenEventName = "errorsViewerOpen";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(ErrorsViewerOpenEventName);
        }
    }
}
