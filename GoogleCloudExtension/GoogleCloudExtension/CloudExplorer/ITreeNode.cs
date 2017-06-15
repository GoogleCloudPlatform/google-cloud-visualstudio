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

using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    public interface ITreeNode
    {
        /// <summary>
        /// Whether this node is in the loading state.
        /// </summary>
        bool IsLoading { get; set; }

        /// <summary>
        /// Whether this node is in the error state.
        /// </summary>
        bool IsError { get; set; }

        /// <summary>
        /// Whether this node is in the warning state.
        /// </summary>
        bool IsWarning { get; set; }

        /// <summary>
        /// Whether the custom node icon is to be used or not, only in normal mode.
        /// </summary>
        bool IconIsVisible { get; }

        /// <summary>
        /// The icon to use in the UI for this item.
        /// </summary>
        ImageSource Icon { get; set; }

        /// <summary>
        /// The content to display for this item.
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        /// The context menu for this item.
        /// </summary>
        ContextMenu ContextMenu { get; set; }

        /// <summary>
        /// Some context menu item needs to be enabled/disabled dynamically.
        /// Override this method to update menu item state.
        /// </summary>
        void OnMenuItemOpen();
    }
}