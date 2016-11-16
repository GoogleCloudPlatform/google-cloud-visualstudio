namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when a GCS bucket is opened on cloud console.
    /// </summary>
    internal static class OpenGcsBucketOnCloudConsoleEvent
    {
        private const string OpenGcsBucketOnCloudConsoleEventName = "openGcsBucketOnCloudConsole";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(OpenGcsBucketOnCloudConsoleEventName);
        }
    }
}
