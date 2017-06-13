namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserStartDeleteEvent
    {
        private const string GcsFileBrowserStartDeleteEventName = "gcsFileBrowserStartDelete";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GcsFileBrowserStartDeleteEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
