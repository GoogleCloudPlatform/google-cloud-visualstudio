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

        public static void Report(CommandStatus status)
        {
            EventsReporterWrapper.ReportEvent(
                FlexAppDeploymentEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
