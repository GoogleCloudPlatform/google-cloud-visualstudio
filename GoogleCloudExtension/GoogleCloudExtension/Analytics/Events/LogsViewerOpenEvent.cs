using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerOpenEvent
    {
        private const string LogsViewerOpenEventName = "logsViewerOpen";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerOpenEventName);
        }
    }
}
