namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserStartDownloadEvent
    {
        private const string GcsFileBrowserDownloadEventName = "gcsFileBrowserStartDownload";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GcsFileBrowserDownloadEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
