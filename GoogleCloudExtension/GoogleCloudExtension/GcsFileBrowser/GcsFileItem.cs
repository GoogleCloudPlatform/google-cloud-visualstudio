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

using GoogleCloudExtension.Utils;
using System.ComponentModel;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsFileItem : PropertyWindowItemBase
    {
        private readonly GcsRow _row;

        public GcsFileItem(GcsRow row) :
            base("GCS File", row.FileName)
        {
            _row = row;
        }

        [LocalizedCategory(nameof(Resources.GcsFileBrowserFileCategory))]
        [LocalizedDisplayName(nameof(Resources.GcsFileBrowserNameDisplayName))]
        [LocalizedDescription(nameof(Resources.GcsFileBrowserFileNameDescription))]
        public string Name => _row.FileName;

        [LocalizedCategory(nameof(Resources.GcsFileBrowserFileCategory))]
        [LocalizedDisplayName(nameof(Resources.GcsFileBrowserFileSizeDisplayName))]
        [LocalizedDescription(nameof(Resources.GcsFileBrowserFileSizeDescription))]
        public string Size => _row.Size.ToString();

        [LocalizedCategory(nameof(Resources.GcsFileBrowserFileCategory))]
        [LocalizedDisplayName(nameof(Resources.GcsFileBrowserFileLastModifiedDisplayName))]
        [LocalizedDescription(nameof(Resources.GcsFileBrowserFileLastModifiedDescription))]
        public string LasModified => _row.LastModified;

        [LocalizedCategory(nameof(Resources.GcsFileBrowserFileCategory))]
        [LocalizedDisplayName(nameof(Resources.GcsFileBrowserFileFullPathDisplayName))]
        [LocalizedDescription(nameof(Resources.GcsFileBrowserFileFullPathDescription))]
        public string GcsPath => $"gs://{_row.Bucket}/{_row.Name}";

        [LocalizedCategory(nameof(Resources.GcsFileBrowserFileCategory))]
        [LocalizedDisplayName(nameof(Resources.GcsFileBrowserFileContentTypeDisplayName))]
        [LocalizedDescription(nameof(Resources.GcsFileBrowserFileContentTypeDescription))]
        public string ContentType => _row.ContentType;
    }
}
