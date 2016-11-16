﻿namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event reported when a new Remote Desktop session is started.
    /// </summary>
    internal static class StartRemoteDesktopSessionEvent
    {
        private const string StartRemoteDesktopSessionEventName = "startRemoteDesktopSession";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(StartRemoteDesktopSessionEventName);
        }
    }
}
