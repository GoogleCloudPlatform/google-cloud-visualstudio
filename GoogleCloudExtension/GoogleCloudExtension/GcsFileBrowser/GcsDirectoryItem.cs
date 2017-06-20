﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// This class represents an directory to be shown in the property window in the VS shell.
    /// </summary>
    internal class GcsDirectoryItem : PropertyWindowItemBase
    {
        private readonly GcsRow _directory;

        public GcsDirectoryItem(GcsRow row) :
            base(Resources.GcsFileBrowserDirectoryItemDisplayName, row.LeafName)
        {
            _directory = row;
        }

        /// <summary>
        /// The name of the directory.
        /// </summary>
        [LocalizedCategory(nameof(Resources.GcsFileBrowserDirectoryCategory))]
        [LocalizedDisplayName(nameof(Resources.GcsFileBrowserNameDisplayName))]
        [LocalizedDescription(nameof(Resources.GcsFileBrowserDirectoryNameDescription))]
        public string Name => _directory.LeafName;

        /// <summary>
        /// The full gs://... path to the directory.
        /// </summary>
        [LocalizedCategory(nameof(Resources.GcsFileBrowserDirectoryCategory))]
        [LocalizedDisplayName(nameof(Resources.GcsFileBrowserFullPathDisplayName))]
        [LocalizedDescription(nameof(Resources.GcsFileBrowserDirectoryFullPathDescription))]
        public string GcsPath => _directory.GcsPath;
    }
}