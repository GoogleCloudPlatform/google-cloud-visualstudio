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
        private const string Category = "Zone Properties";

        private readonly Zone _zone;

        public ZoneItem(Zone zone) : base(className: Category, componentName: zone.Name)
        {
            _zone = zone;
        }


        [Category(Category)]
        [Description("The name of the zone.")]
        public string Name => _zone.Name;

        [Category(Category)]
        [Description("The description of the zone.")]
        public string Description => _zone.Description;

        [Category(Category)]
        [Description("The region for the zone.")]
        public string Region => _zone.Region;

        [Category(Category)]
        [Description("The status of the zone.")]
        public string Status => _zone.Status;
    }
}
