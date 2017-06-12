using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class LogsViewerSimpleTextSearchEvent
    {
        private const string LogsViewerSimpleTextSearchEventName = "logsViewersimpleTextSearch";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(LogsViewerSimpleTextSearchEventName);
        }
    }
}
