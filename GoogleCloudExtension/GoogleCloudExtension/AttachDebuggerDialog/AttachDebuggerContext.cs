// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtension.Utils;
using System;
using static GoogleCloudExtension.Utils.ArgumentCheckUtils;
using static GoogleCloudExtension.VsVersion.VsVersionUtils;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// This class contains common data for attach remote debugger steps.
    /// It is in particular useful for passing some state, data among steps.
    /// The context is valid when a AttachDebuggerWindow is shown, 
    /// and become invalid when the dialog dissmissed.
    /// </summary>
    public class AttachDebuggerContext
    {
        private readonly Lazy<GceDataSource> _lazyDataSource = new Lazy<GceDataSource>(CreateDataSource);

        /// <summary>
        /// The <seealso cref="PortInfo"/> for remote PowerShell HTTPs port.
        /// </summary>
        public static PortInfo RemotePowerShellPortInfo { get; } =
            new PortInfo("HTTPSRemotePowerShell", 5986, description: Resources.PortManagerRemotePowershellDescription);

        /// <summary>
        /// The <seealso cref="PortInfo"/> for Visual Studio Remote Debugger tool port.
        /// </summary>
        public static readonly PortInfo DebuggerPortInfo =
            new PortInfo("VSRemoteDebugger", RemoteDebuggerPort, Resources.PortManagerRemoteDebuggerDescription);

        /// <summary>
        /// The username chosen for the GCE instance.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password chosen for the GCE instance.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets the <seealso cref="DataSource"/> object.
        /// </summary>
        public GceDataSource DataSource => _lazyDataSource.Value;

        /// <summary>
        /// The attaching dialog window that is the container of all steps.
        /// </summary>
        public AttachDebuggerWindow DialogWindow { get; }

        /// <summary>
        /// The GCE VM <seealso cref="Instance"/> object.
        /// </summary>
        public Instance GceInstance { get; private set; }

        /// <summary>
        /// The GCE VM instance public ip address.
        /// </summary>
        public string PublicIp { get; }

        /// <summary>
        /// The Visual Studio Remote Debugger tool port. 
        /// </summary>
        public AttachDebuggerFirewallPort DebuggerPort { get; }

        /// <summary>
        /// The remote PowerShell HTTPs port.
        /// </summary>
        public AttachDebuggerFirewallPort RemotePowerShellPort { get; }

        /// <summary>
        /// Create the <seealso cref="Context"/> to associate the <paramref name="gceInstance"/>.
        /// </summary>
        /// <param name="gceInstance">GCE VM instance object.</param>
        /// <param name="dialogWindow">The dialog window</param>
        public AttachDebuggerContext(Instance gceInstance, AttachDebuggerWindow dialogWindow)
        {
            GceInstance = gceInstance.ThrowIfNull(nameof(gceInstance));
            DialogWindow = dialogWindow.ThrowIfNull(nameof(dialogWindow));
            PublicIp = gceInstance.GetPublicIpAddress();
            DebuggerPort = new AttachDebuggerFirewallPort(DebuggerPortInfo, gceInstance, _lazyDataSource);
            RemotePowerShellPort = new AttachDebuggerFirewallPort(RemotePowerShellPortInfo, gceInstance, _lazyDataSource);
        }

        private static GceDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new GceDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }
    }
}
