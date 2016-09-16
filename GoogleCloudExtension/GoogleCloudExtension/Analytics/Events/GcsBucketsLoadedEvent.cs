using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsBucketsLoadedEvent
    {
        private const string GcsBucketsLoadedEventName = "gcsBucketsLoad";

        public static void Report(CommandStatus status)
        {
            EventsReporterWrapper.ReportEvent(
                GcsBucketsLoadedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
