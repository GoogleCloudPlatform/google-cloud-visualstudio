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

using System.Collections.ObjectModel;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class represetns a node in the tree that contains children nodes.
    /// </summary>
    public class TreeHierarchy : TreeNode
    {
        private bool _isExpanded;

        /// <summary>
        /// The children for this item.
        /// </summary>
        public ObservableCollection<TreeNode> Children { get; } = new ObservableCollection<TreeNode>();

        /// <summary>
        /// Returns whether the hierarchy is expanded or not.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    SetValueAndRaise(ref _isExpanded, value);
                    OnIsExpandedChanged(value);
                }
            }
        }

        /// <summary>
        /// This method will be called every time the value of IsExpanded property changes.
        /// </summary>
        /// <param name="newValue">The new value of the <seealso cref="IsExpanded"/> property.</param>
        protected virtual void OnIsExpandedChanged(bool newValue)
        { }
    }
}
