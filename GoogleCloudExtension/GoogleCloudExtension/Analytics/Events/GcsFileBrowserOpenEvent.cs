using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserOpenEvent
    {
        private const string GcsFileBrowserOpenEventName = "gcsFileBrowserOpen";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(GcsFileBrowserOpenEventName);
        }
    }
}
