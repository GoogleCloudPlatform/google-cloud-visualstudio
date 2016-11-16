using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent when the user opens the Firewall options dialog.
    /// </summary>
    internal class OpenFirewallPortsDialogEvent
    {
        private const string OpenFirewallPortsDialogEventName = "openFirewallPortsDialog";

        public static AnalyticsEvent Create()
        {
            return new AnalyticsEvent(OpenFirewallPortsDialogEventName);
        }
    }
}
