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

using System;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// This class represents an operation to move (or rename) a GCS file within the same bucket.
    /// </summary>
    public class GcsMoveFileOperation : GcsOperation
    {
        /// <summary>
        /// The reference to the GCS file that is going to be renamed.
        /// </summary>
        public override GcsItemRef GcsItem { get; }

        /// <summary>
        /// The reference to the new name to use for the file.
        /// </summary>
        public GcsItemRef ToItem { get; }

        public GcsMoveFileOperation(GcsItemRef fromItem, GcsItemRef toItem)
        {
            if (fromItem.Bucket != toItem.Bucket)
            {
                throw new InvalidOperationException("Can only move items within the same bucket.");
            }

            GcsItem = fromItem;
            ToItem = toItem;
        }
    }
}
