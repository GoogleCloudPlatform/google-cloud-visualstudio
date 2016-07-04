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
using GoogleCloudExtension.Utils;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class ZoneItem : PropertyWindowItemBase
    {
        private readonly Zone _zone;

        public ZoneItem(Zone zone) : base(className: Resources.CloudExplorerGceZoneCategory, componentName: zone.Name)
        {
            _zone = zone;
        }


        [LocalizedCategory(nameof(Resources.CloudExplorerGceZoneCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceZoneNameDescription))]
        public string Name => _zone.Name;

        [LocalizedCategory(nameof(Resources.CloudExplorerGceZoneCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceZoneDescriptionDescription))]
        public string Description => _zone.Description;

        [LocalizedCategory(nameof(Resources.CloudExplorerGceZoneCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceZoneRegionDescription))]
        public string Region => _zone.Region;

        [LocalizedCategory(nameof(Resources.CloudExplorerGceZoneCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceZoneStatusDescription))]
        public string Status => _zone.Status;
    }
}
