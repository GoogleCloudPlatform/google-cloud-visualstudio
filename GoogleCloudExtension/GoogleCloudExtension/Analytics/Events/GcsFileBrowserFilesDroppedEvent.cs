namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserFilesDroppedEvent
    {
        private const string GcsFileBrowserFilesDroppedEventName = "gcsFileBrowserFilesDropped";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(GcsFileBrowserFilesDroppedEventName);
        }
    }
}
