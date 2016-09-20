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

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(NewInstallEventName);
        }
    }
}
