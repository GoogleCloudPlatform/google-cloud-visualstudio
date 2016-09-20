using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class NewInstallEvent
    {
        private const string NewInstallEventName = "newInstall";
        private const string VersionProperty = "version";

        public static AnalyticsEvent Create(string version)
        {
            return new AnalyticsEvent(
                NewInstallEventName,
                VersionProperty, version);
        }
    }
}
