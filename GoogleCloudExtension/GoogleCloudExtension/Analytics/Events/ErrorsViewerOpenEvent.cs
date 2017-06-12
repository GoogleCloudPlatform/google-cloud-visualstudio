using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class ErrorsViewerOpenEvent
    {
        private const string ErrorsViewerOpenEventName = "errorsViewerOpen";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(ErrorsViewerOpenEventName);
        }
    }
}
