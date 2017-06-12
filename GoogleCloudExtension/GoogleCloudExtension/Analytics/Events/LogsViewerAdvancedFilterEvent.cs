using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerAdvancedFilterEvent
    {
        private const string LogsViewerAdvancedFilterEventName = "logsViewerAdvancedFilter";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerAdvancedFilterEventName);
        }
    }
}
