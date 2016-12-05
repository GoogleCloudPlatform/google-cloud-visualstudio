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

        [Category("File")]
        [DisplayName("Name")]
        [Description("Name of the blob.")]
        public string Name => _row.FileName;

        [Category("File")]
        [DisplayName("Size")]
        [Description("Size of the blob in bytes.")]
        public string Size => _row.Size.ToString();

        [Category("File")]
        [DisplayName("Last Modified")]
        [Description("The last modified time stamp of the blob.")]
        public string LasModified => _row.LastModified;

        [Category("File")]
        [DisplayName("GCS Path")]
        [Description("The full path to the blob.")]
        public string GcsPath => $"gs://{_row.Bucket}/{_row.Name}";
           
    }
}
