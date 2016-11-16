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


using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a GAE instance in the Properties Window.
    /// </summary>
    internal class InstanceItem : PropertyWindowItemBase
    {
        private readonly Instance _instance;

        public InstanceItem(Instance instance) : base(className: Resources.CloudExplorerGaeInstanceCategory, componentName: instance.Id)
        {
            _instance = instance;
        }

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public string Name => _instance.Name;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public string Id => _instance.Id;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public string Availability => _instance.Availability;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public int? Requests => _instance.Requests;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public int? Errors => _instance.Errors;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public float? QPS => _instance.Qps;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceStartTimeDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public string StartTime => _instance.StartTime;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceAppEngineReleaseDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public string AppEngineRelease => _instance.AppEngineRelease;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceMemoryUsageDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public long? MemoryUsage => _instance.MemoryUsage;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceAverageLatencyDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceCategory))]
        public int? AverageLatency => _instance.AverageLatency;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineNameDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineCategory))]
        public string VmName => _instance.VmName;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineZoneDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineCategory))]
        public string VmZone => _instance.VmZoneName;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineIdDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineCategory))]
        public string VmId => _instance.VmId;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineStatusDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeInstanceVirtualMachineCategory))]
        public string VmStatus => _instance.VmStatus;

        public override string ToString() => _instance.VmName;
    }
}
