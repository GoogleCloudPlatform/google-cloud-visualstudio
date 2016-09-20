using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    internal static class ChangedFirewallPortsEvent
    {
        private const string ChangedFirewallPortsEventName = "manageFirewallPorts";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                ChangedFirewallPortsEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
