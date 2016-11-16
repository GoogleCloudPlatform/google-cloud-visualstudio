namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when a set of Windows credentials is added.
    /// </summary>
    internal static class AddWindowsCredentialEvent
    {
        private const string AddWindowsCredentialEventName = "addWindowsCredential";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(AddWindowsCredentialEventName);
        }
    }
}
