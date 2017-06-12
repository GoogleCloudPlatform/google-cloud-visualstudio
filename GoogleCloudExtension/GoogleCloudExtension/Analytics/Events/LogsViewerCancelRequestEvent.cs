using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerCancelRequestEvent
    {
        private const string LogsViewerCancelRequestEventName = "logsViewerCancelRequest";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerCancelRequestEventName);
        }
    }
}
