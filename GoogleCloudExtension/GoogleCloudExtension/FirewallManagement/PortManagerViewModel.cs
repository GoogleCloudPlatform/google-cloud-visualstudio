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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
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

        public PortChanges(IList<FirewallPort> portsToEnable, IList<FirewallPort> portsToDisable)
        {
            PortsToEnable = portsToEnable;
            PortsToDisable = portsToDisable;
        }
    }

    /// <summary>
    /// This class is the view model for the <seealso cref="PortManagerWindow"/> dialog.
    /// </summary>
    public class PortManagerViewModel : ViewModelBase
    {
        /// <summary>
        /// The list of supported ports in the dialog, in the same order that they will be
        /// offered to the user.
        /// </summary>
        private static readonly IList<PortInfo> s_supportedPorts = new List<PortInfo>
        {
            new PortInfo("HTTP", 80),
            new PortInfo("HTTPS", 443),
            new PortInfo("RDP", 3389),
            new PortInfo("WebDeploy", 8172),
        };

        private readonly PortManagerWindow _owner;
        private readonly Instance _instance;

        /// <summary>
        /// The list of ports.
        /// </summary>
        public IList<PortModel> Ports { get; }

        /// <summary>
        /// The command to execute when the Ok button is pressed.
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// The changes that were selected by the user. This property will be null if the user
        /// cancelled the operation.
        /// </summary>
        public PortChanges Result { get; private set; }

        public PortManagerViewModel(PortManagerWindow owner, Instance instance)
        {
            _owner = owner;
            _instance = instance;

            Ports = CreatePortModels();
            OkCommand = new ProtectedCommand(OnOkCommand);
        }

        private void OnOkCommand()
        {
            var portsToEnable = new List<FirewallPort>();
            var portsToDisable = new List<FirewallPort>();

            foreach (var entry in Ports.Where(x => x.Changed))
            {
                var firewallPort = new FirewallPort(entry.PortInfo.GetTag(_instance), entry.Port);
                if (entry.IsEnabled)
                {
                    portsToEnable.Add(firewallPort);
                }
                else
                {
                    portsToDisable.Add(firewallPort);
                }
            }

            Result = new PortChanges(portsToEnable: portsToEnable, portsToDisable: portsToDisable);

            _owner.Close();
        }

        private IList<PortModel> CreatePortModels() =>
            s_supportedPorts.Select((x) => new PortModel(x, _instance.Tags?.Items?.Contains(x.GetTag(_instance)) ?? false)).ToList();
    }
}
