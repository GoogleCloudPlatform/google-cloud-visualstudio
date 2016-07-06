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
    /// This class represents a GCE instance that is serving App Engine traffic in the 
    /// Properties window.
    /// </summary>
    public class GceGaeInstanceItem : GceInstanceItem
    {
        public GceGaeInstanceItem(Instance instance) : base(instance)
        { }

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceAppEngineCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceModuleDescription))]
        public string Module => Instance.GetAppEngineFlexService();

        [LocalizedCategory(nameof(Resources.CloudExplorerGceInstanceAppEngineCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGceInstanceVersionDescription))]
        public string Version => Instance.GetAppEngineFlexVersion();

        public override string ToString() => Instance.Name;
    }
}
