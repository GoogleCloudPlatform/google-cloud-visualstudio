namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserStartRenameDirectoryEvent
    {
        private const string GcsFileBrowserStartRenameDirectoryEventName = "gcsFileBrowserStartRenameDirectory";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GcsFileBrowserStartRenameDirectoryEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
