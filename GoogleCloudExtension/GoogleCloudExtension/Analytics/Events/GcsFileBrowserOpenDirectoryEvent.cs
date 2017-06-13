namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserOpenDirectoryEvent
    {
        private const string GcsFileBrowserOpenDirectoryEventName = "gcsFileBrowserOpenDirectory";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(GcsFileBrowserOpenDirectoryEventName);
        }
    }
}
