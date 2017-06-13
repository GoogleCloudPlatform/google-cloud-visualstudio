namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserOpenEvent
    {
        private const string GcsFileBrowserOpenEventName = "gcsFileBrowserOpen";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(GcsFileBrowserOpenEventName);
        }
    }
}
