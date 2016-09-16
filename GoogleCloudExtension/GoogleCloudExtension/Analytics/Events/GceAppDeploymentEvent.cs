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
        private const string StatusProperty = "status";

        public static void Report(string status)
        {
            EventsReporterWrapper.ReportEvent(
                GceAppDeploymentEventName,
                StatusProperty, status);
        }
    }
}
