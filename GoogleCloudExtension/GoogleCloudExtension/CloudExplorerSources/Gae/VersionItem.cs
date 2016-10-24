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
    /// This class represents a GAE service in the Properties Window.
    /// </summary>
    class VersionItem : PropertyWindowItemBase
    {

        private readonly Version _version;

        public VersionItem(Version version) : base(className: Resources.CloudExplorerGaeVersionCategory, componentName: version.Id)
        {
            _version = version;   
        }

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Name => _version.Name;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Id => _version.Id;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Status => _version.ServingStatus;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Deployer => _version.CreatedBy;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Url => _version.VersionUrl;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Runtime => _version.Runtime;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Environment => _version.Env;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionInstanceClassDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string InstanceClass => _version.InstanceClass;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionCreationTimeDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string CreationTime => _version.CreateTime;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionVirtualMachineDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public bool? VirtualMachine => _version.Vm;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionResourcesCategory))]
        public double? CPU => _version.Resources?.Cpu;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionResoucesDiskDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionResourcesCategory))]
        public double? Disk => _version.Resources?.DiskGb;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionResoucesMemoryDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionResourcesCategory))]
        public double? Memory => _version.Resources?.MemoryGb;

        public override string ToString() => _version.Id;
    }
}
