using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class FlexAppDeploymentEvent
    {
        private const string FlexAppDeploymentEventName = "flexAppDeployment";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                FlexAppDeploymentEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
