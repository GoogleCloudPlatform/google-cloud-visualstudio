﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.CloudExplorer;
using System;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Source class for the Pubsub tree.
    /// </summary>
    internal class PubsubSource : CloudExplorerSourceBase<PubsubSourceRootViewModel>
    {
        public PubsubSource(ICloudSourceContext context) : base(context) { }

        /// <summary>
        /// Gets the last part of the full name i.e. the leaf of the path.
        /// </summary>
        internal static string GetPathLeaf(string path)
        {
            return path.Substring(1 + path.LastIndexOf("/", StringComparison.Ordinal));
        }
    }
}
