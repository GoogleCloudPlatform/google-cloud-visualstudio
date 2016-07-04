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
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    /// <summary>
    /// This class represents a GCE instance in the Properties Window.
    /// </summary>
    public class GceInstanceItem : PropertyWindowItemBase
    {
        protected Instance Instance { get; }

        public GceInstanceItem(Instance instance)
            : base(className: Resources.CloudExplorerGceInstanceCategory, componentName: instance.Name)
        {
            Instance = instance;
        }

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceNameDescription))]
        public string Name => Instance.Name;

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceZoneDescription))]
        public string Zone => Instance.GetZoneName();

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceMachineTypeDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerGceInstanceMachineTypeDisplayName))]
        public string MachineType => Instance.GetMachineType();

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceStatusDescription))]
        public string Status => Instance.Status;

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceInternalIpDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerGceInstanceInternalIpDisplayName))]
        public string IpAddress => Instance.GetInternalIpAddress();

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstancePublicIpDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerGceInstancePublicIpDisplayName))]
        public string PublicIpAddress => Instance.GetPublicIpAddress();

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceTagsDescription))]
        public string Tags => Instance.GetTags();
    }
}