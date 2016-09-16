using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class NewLoginEvent
    {
        private const string NewLoginEventName = "newLogin";

        public static void Report()
        {
            EventsReporterWrapper.ReportEvent(NewLoginEventName);
        }
    }
}
