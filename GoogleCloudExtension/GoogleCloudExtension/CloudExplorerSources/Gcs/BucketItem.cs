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

using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class BucketItem : PropertyWindowItemBase
    {
        private readonly Bucket _bucket;

        public BucketItem(Bucket bucket) : base(className: Resources.CloudExplorerGcsBucketCategory, componentName: bucket.Name)
        {
            _bucket = bucket;
        }

        [LocalizedCategory(nameof(Resources.CloudExplorerGcsBucketCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGcsBucketNameDescription))]
        public string Name => _bucket.Name;

        [LocalizedCategory(nameof(Resources.CloudExplorerGcsBucketCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGcsBucketCreatedDescription))]
        public string Created => _bucket.TimeCreated?.ToShortDateString();

        [LocalizedCategory(nameof(Resources.CloudExplorerGcsBucketCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGcsBucketModifiedDescription))]
        public string Updated => _bucket.Updated?.ToShortDateString();

        [LocalizedCategory(nameof(Resources.CloudExplorerGcsBucketCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGcsBucketVersioningEnabledDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerGcsBucketVersioningEnabledDisplayName))]
        public bool VersioningEnabled => _bucket?.Versioning?.Enabled ?? false;

        [LocalizedCategory(nameof(Resources.CloudExplorerGcsBucketCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerGcsBucketStorageClassDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerGcsBucketStorageClassDisplayName))]
        public string StorageClass => _bucket.StorageClass;

        public override string ToString() => _bucket.Name;
    }
}