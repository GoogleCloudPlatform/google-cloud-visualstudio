namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserStartUploadEvent
    {
        private const string GcsFileBrowserStartUploadEventName = "gcsFileBrowserStartUpload";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GcsFileBrowserStartUploadEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
