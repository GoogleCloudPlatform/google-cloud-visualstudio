using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics
{
    internal static class AnalyticEvents
    {
        public const string ExtensionEventType = "vsextension";

        public const string NewInstallEventName = "newInstall";
        public const string UpgradeEventName = "upgrade";
        public const string NewLoginEventName = "newLogin";
        public const string CloudExplorerInteractionEventName = "cloudExplorerInteraction";
        public const string GceAppDeploymentEventName = "gceAppDeployment";
        public const string FlexAppDeploymentEventName = "flexAppDeployment";
        public const string GceVMListLoadEventName = "gceVMsLoad";
        public const string GcsBucketListLoadEventName = "gcsBucketsLoad";
        public const string CloudSQLInstancesListLoadEventName = "cloudSQLInstancesLoad";
        public const string ChangedFirewallPortsEventName = "manageFirewallPorts";
    }
}
