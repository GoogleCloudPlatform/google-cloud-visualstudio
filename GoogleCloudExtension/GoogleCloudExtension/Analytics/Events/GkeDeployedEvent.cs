using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent after a deployment to GKE.
    /// </summary>
    internal static class GkeDeployedEvent
    {
        private const string GkeDeployedEventName = "gkeDeployment";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GkeDeployedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
