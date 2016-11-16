namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when the user opens a GCE instance in Cloud Console.
    /// </summary>
    internal static class OpenGceInstanceOnCloudConsoleEvent
    {
        private const string OpenGceInstanceOnCloudConsoleEventName = "openGceInstanceOnCloudConsole";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(OpenGceInstanceOnCloudConsoleEventName);
        }
    }
}
