using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class GcsFileBrowserFilesDroppedEvent
    {
        private const string GcsFileBrowserFilesDroppedEventName = "gcsFileBrowserFilesDropped";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(GcsFileBrowserFilesDroppedEventName);
        }
    }
}
