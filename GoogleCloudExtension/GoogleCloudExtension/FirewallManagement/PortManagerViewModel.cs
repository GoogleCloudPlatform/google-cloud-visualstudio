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
    public class PortChanges
    {
        public IList<FirewallPort> PortsToEnable { get; }

        public IList<FirewallPort> PortsToDisable { get; }

        public bool HasChanges => PortsToEnable.Count != 0 || PortsToDisable.Count != 0;

        public PortChanges(IList<FirewallPort> portsToEnable, IList<FirewallPort> portsToDisable)
        {
            PortsToEnable = portsToEnable;
            PortsToDisable = portsToDisable;
        }
    }

    public class PortManagerViewModel : ViewModelBase
    {
        private static readonly IList<PortInfo> s_supportedPorts = new List<PortInfo>
        {
            new PortInfo("HTTP", 80),
            new PortInfo("HTTPS", 443),
            new PortInfo("RDP", 3389),
            new PortInfo("WebDeploy", 8172),
        };

        private readonly PortManagerWindow _owner;
        private readonly Instance _instance;
        private readonly GceDataSource _dataSource;
        private bool _savingChanges;

        public IList<PortModel> Ports { get; }

        public bool SavingChanges
        {
            get { return _savingChanges; }
            set
            {
                SetValueAndRaise(ref _savingChanges, value);
                RaisePropertyChanged(nameof(NotSavingChanges));
            }
        }

        public bool NotSavingChanges => !SavingChanges;

        public ICommand OkCommand { get; }

        public PortChanges Result { get; private set; }

        public PortManagerViewModel(PortManagerWindow owner, Instance instance, GceDataSource dataSource)
        {
            _owner = owner;
            _instance = instance;
            _dataSource = dataSource;

            Ports = CreatePortModels();
            OkCommand = new WeakCommand(OnOkCommand);
        }

        private async void OnOkCommand()
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

        

        private GceOperation UpdateInstanceTags(
            IEnumerable<PortInfo> portsToEnable,
            IEnumerable<PortInfo> portsToDisable)
        {
            var tagsToAdd = portsToEnable.Select(x => x.GetTag(_instance));
            var tagsToRemove = portsToDisable.Select(x => x.GetTag(_instance));

            return _dataSource.SetInstanceTags(
                _instance,
                _instance.Tags.Items.Except(tagsToRemove).Union(tagsToAdd).ToList());
        }

        private void OnCancelCommand()
        {
            _owner.Close();
        }

        private IList<PortModel> CreatePortModels() =>
            s_supportedPorts.Select((x) => new PortModel(x, _instance.Tags?.Items.Contains(x.GetTag(_instance)) ?? false)).ToList();
    }
}
