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
    internal class VersionItem : PropertyWindowItemBase
    {
        private readonly Version _version;

        public VersionItem(Version version) : base(className: Resources.CloudExplorerGaeVersionCategory, componentName: version.Id)
        {
            _version = version;
        }

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        [LocalizedDisplayName(nameof(Resources.PropertyWindowItemNameDisplayName))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGaeVersionNameDescription))]
        public string Name => _version.Name;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        [LocalizedDisplayName(nameof(Resources.PropertyWindowItemIdDisplayName))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGaeVersionIdDescription))]
        public string Id => _version.Id;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionServingStatusDisplayName))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGaeVersionServingStatusDescription))]
        public string ServingStatus => _version.ServingStatus;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionCreatedByDisplayName))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGaeVersionCreatedByDescription))]
        public string CreatedBy => _version.CreatedBy;

        [LocalizedDisplayName(nameof(Resources.CloudExplorerGaeVersionCreationTimeDisplayName))]
        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGaeVersionCreateTimeDescription))]
        public string CreateTime => _version.CreateTime;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Url => _version.VersionUrl;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Runtime => _version.Runtime;

        [LocalizedCategory(nameof(Resources.CloudExplorerGaeVersionCategory))]
        public string Environment => _version.Env;

        public override string ToString() => _version.Id;
    }
}
