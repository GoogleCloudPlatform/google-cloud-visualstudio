using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class UpgradeEvent
    {
        private const string UpgradeEventName = "upgrade";
        private const string VersionProperty = "version";

        public static void Report(string version)
        {
            EventsReporterWrapper.ReportEvent(
                UpgradeEventName,
                VersionProperty, version);
        }
    }
}
