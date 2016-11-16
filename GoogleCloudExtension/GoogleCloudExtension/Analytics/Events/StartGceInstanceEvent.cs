using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// Event sent when the GCE instance is started.
    /// </summary>
    internal static class StartGceInstanceEvent
    {
        private const string StartGceInstanceEventName = "startGceInstance";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                StartGceInstanceEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
