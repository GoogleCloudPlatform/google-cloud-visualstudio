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

using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// Interface to be implemented by cloud explorer data sources.
    /// </summary>
    public interface ICloudExplorerSource
    {
        /// <summary>
        /// Returns the root of the hierarchy for this source.
        /// </summary>
        TreeHierarchy Root { get; }

        /// <summary>
        /// Returns the buttons, if any, defined by the source.
        /// </summary>
        IEnumerable<ButtonDefinition> Buttons { get; }

        /// <summary>
        /// Called when the sources need to reload their data.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Called when the credentials or project changes.
        /// </summary>
        void InvalidateProjectOrAccount();
    }
}
