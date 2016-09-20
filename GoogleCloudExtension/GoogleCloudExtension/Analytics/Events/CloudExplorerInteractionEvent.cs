using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class CloudExplorerInteractionEvent
    {
        private const string CloudExplorerInteractionEventName = "cloudExplorerInteraction";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(CloudExplorerInteractionEventName);
        }
    }
}
