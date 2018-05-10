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

using Google.Apis.CloudResourceManager.v1.Data;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// The context information for a Cloud Explorer source.
    /// </summary>
    public interface ICloudSourceContext
    {
        /// <summary>
        /// The currently selected project.
        /// </summary>
        Project CurrentProject { get; }

        /// <summary>
        /// Shows the properties window for the provided item.
        /// </summary>
        /// <param name="item"></param>
        void ShowPropertiesWindow(object item);
    }
}
