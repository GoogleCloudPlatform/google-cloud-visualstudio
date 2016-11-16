namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when a .publishsettings file is created.
    /// </summary>
    internal static class SavePublishSettingsEvent
    {
        private const string SavePublishSettingsEventName = "savePublishSettings";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(SavePublishSettingsEventName);
        }
    }
}
