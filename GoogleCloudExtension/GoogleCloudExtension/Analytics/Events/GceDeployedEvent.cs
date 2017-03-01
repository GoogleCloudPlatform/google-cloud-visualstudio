using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent after a deployment to a GCE VM.
    /// </summary>
    internal static class GceDeployedEvent
    {
        private const string GceDeployedEventName = "gceDeployment";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GceDeployedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
