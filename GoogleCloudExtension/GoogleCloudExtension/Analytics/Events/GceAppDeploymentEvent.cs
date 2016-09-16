using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GceAppDeploymentEvent
    {
        private const string GceAppDeploymentEventName = "gceAppDeployment";

        public static void Report(CommandStatus status)
        {
            EventsReporterWrapper.ReportEvent(
                GceAppDeploymentEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
