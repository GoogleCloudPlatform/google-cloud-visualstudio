namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when the cloud SQL connection dialog is open.
    /// </summary>
    internal static class OpenCloudSqlConnectionDialogEvent
    {
        private const string OpenCloudSqlConnectionDialogEventName = "openCloudSqlConnectionDialog";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(OpenCloudSqlConnectionDialogEventName);
        }
    }
}
