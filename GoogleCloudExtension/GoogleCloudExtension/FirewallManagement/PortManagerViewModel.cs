// Copyright 2016 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.AttachDebuggerDialog;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.FirewallManagement
{
    /// <summary>
    /// The changes selected by the user in the <seealso cref="PortManagerWindow"/> dialog.
    /// </summary>
    public class PortChanges
    {
        /// <summary>
        /// The list of ports to open.
        /// </summary>
        public IList<FirewallPort> PortsToEnable { get; }

        /// <summary>
        /// The list of ports to close.
        /// </summary>
        public IList<FirewallPort> PortsToDisable { get; }

        /// <summary>
        /// Whether the are changes stored in this instance.
        /// </summary>
        public bool HasChanges => PortsToEnable.Count != 0 || PortsToDisable.Count != 0;

        public PortChanges(IEnumerable<PortModel> changedPorts)
        {
            PortsToEnable = new List<FirewallPort>();
            PortsToDisable = new List<FirewallPort>();

            foreach (PortModel entry in changedPorts ?? Enumerable.Empty<PortModel>())
            {
                var firewallPort = new FirewallPort(entry.GetPortInfoTag(), entry.PortInfo.Port);
                if (entry.IsEnabled)
                {
                    PortsToEnable.Add(firewallPort);
                }
                else
                {
                    PortsToDisable.Add(firewallPort);
                }
            }
        }
    }

    /// <summary>
    /// This class is the view model for the <seealso cref="PortManagerWindow"/> dialog.
    /// </summary>
    public class PortManagerViewModel : ViewModelBase
    {
        public const string ConsoleFirewallsUrlFormat = "https://console.cloud.google.com/networking/firewalls?project={0}";

        /// <summary>
        /// The list of supported ports in the dialog, in the same order that they will be
        /// offered to the user.
        /// </summary>
        internal static readonly IList<PortInfo> s_supportedPorts = new List<PortInfo>
        {
            new PortInfo("HTTP", 80, Resources.PortManagerHttpDescription),
            new PortInfo("HTTPS", 443, Resources.PortManagerHttpsDescription),
            new PortInfo("RDP", 3389, Resources.PortManagerRdpDescription),
            new PortInfo("WebDeploy", 8172, Resources.PortManagerWebDeployDescription),
            AttachDebuggerContext.DebuggerPortInfo,
            AttachDebuggerContext.RemotePowerShellPortInfo,
        };

        private readonly Action _close;
        private readonly Lazy<IBrowserService> _browserService;
        private readonly Lazy<ICredentialsStore> _credentialsStore;

        /// <summary>
        /// The list of ports.
        /// </summary>
        public IList<PortModel> Ports { get; }

        /// <summary>
        /// The command to execute when the Ok button is pressed.
        /// </summary>
        public ICommand OkCommand { get; }

        public ICommand NavigateToCloudConsoleCommand { get; }

        /// <summary>
        /// The changes that were selected by the user. This property will be null if the user
        /// cancelled the operation.
        /// </summary>
        public PortChanges Result { get; private set; }

        public PortManagerViewModel(Action close, Instance instance)
        {
            _close = close;

            Ports = s_supportedPorts.Select(x => new PortModel(x, instance)).ToList();

            OkCommand = new ProtectedCommand(OnOkCommand);
            NavigateToCloudConsoleCommand = new ProtectedCommand(NavigateToCloudConsoleFirewalls);

            _browserService = GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IBrowserService>();
            _credentialsStore = GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<ICredentialsStore>();
        }

        private void NavigateToCloudConsoleFirewalls()
        {
            string url = string.Format(ConsoleFirewallsUrlFormat, _credentialsStore.Value.CurrentProjectId);
            _browserService.Value.OpenBrowser(url);
        }

        private void OnOkCommand()
        {

            Result = new PortChanges(Ports.Where(x => x.Changed));
            _close();
        }
    }
}
