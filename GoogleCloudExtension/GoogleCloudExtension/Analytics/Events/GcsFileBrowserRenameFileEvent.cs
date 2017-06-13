﻿namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserRenameFileEvent
    {
        private const string GcsFileBrowserRenameFileEventName = "gcsFileBrowserRenameFile";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GcsFileBrowserRenameFileEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
