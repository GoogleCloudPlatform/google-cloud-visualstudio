// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// This class represents an operation in flight to transfer data to/from the local
    /// file system and GCS.
    /// </summary>
    public class GcsFileTransferOperation : GcsOperation
    {
        /// <summary>
        /// The path to the source of the operation.
        /// </summary>
        public string LocalPath { get; }

        public override GcsItemRef GcsItem { get; }


        public GcsFileTransferOperation(string localPath, GcsItemRef gcsItem)
        {
            LocalPath = localPath;
            GcsItem = gcsItem;
        }

        public GcsFileTransferOperation(GcsItemRef gcsItem) : this(null, gcsItem)
        { }
    }
}
