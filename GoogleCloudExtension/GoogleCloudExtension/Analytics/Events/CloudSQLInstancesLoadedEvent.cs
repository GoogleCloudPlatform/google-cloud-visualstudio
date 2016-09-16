using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class CloudSQLInstancesLoadedEvent
    {
        private const string CloudSQLInstancesLoadedEventName = "cloudSQLInstancesLoad";

        public static void Report()
        {
            EventsReporterWrapper.ReportEvent(CloudSQLInstancesLoadedEventName);
        }
    }
}
