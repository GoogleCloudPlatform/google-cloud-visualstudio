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

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(UpgradeEventName);
        }
    }
}
