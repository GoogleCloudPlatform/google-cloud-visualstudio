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

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Resource value menu item view mode.
    /// </summary>
    public class ResourceValueItemViewModel : MenuItemViewModel
    {
        /// <summary>
        /// Gets the resource key value.
        /// Example: key is module_id,  the values can be "defaultservice", "my-test-service" etc.
        /// </summary>
        public string ResourceValue { get; }

        /// <summary>
        /// Initializes an instance of <seealso cref="ResourceValueItemViewModel"/> class.
        /// </summary>
        /// <param name="resourceKeyValue">The resource value.</param>
        /// <param name="parent">The parent menu item.</param>
        /// <param name="displayName">
        /// The display name of the resource key value.
        /// Example:  instance_id value is integer as string.  The display name is the instance name.
        /// </param>
        public ResourceValueItemViewModel(
            string resourceKeyValue, 
            MenuItemViewModel parent, 
            string displayName = null) :  base(parent)
        {
            ResourceValue = resourceKeyValue;
            Header = displayName ?? ResourceValue;
        }
    }
}
