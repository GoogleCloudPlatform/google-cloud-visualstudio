namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when the authorized networks for a Cloud SQL instance are changed.
    /// </summary>
    internal static class ManageCloudSqlAuthorizedNetworkEvent
    {
        private const string ManageCloudSqlAuthorizedNetworkEventName = "manageCloudSqlAuthorizedNetwork";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                ManageCloudSqlAuthorizedNetworkEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
