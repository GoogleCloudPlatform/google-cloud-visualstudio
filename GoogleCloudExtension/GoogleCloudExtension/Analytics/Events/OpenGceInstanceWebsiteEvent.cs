namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when the website for a GCE VM is opened.
    /// </summary>
    internal static class OpenGceInstanceWebsiteEvent
    {
        private const string OpenGceInstanceWebsiteEventName = "openGceInstanceWebsite";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(OpenGceInstanceWebsiteEventName);
        }
    }
}
