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
    /// This is the base class for all sources of data for the Cloud Explorer. Implements all of the basic
    /// behaviors, such as loading data, refreshing the data, etc...
    /// </summary>
    /// <typeparam name="TRootViewModel">The type of the root view model for the data source.</typeparam>
    public abstract class CloudExplorerSourceBase<TRootViewModel> : ICloudExplorerSource where TRootViewModel : SourceRootViewModelBase, new()
    {
        /// <summary>
        /// The root of the hierarchy for this data source.
        /// </summary>
        public TreeHierarchy Root => ActualRoot;

        /// <summary>
        /// The buttons for this data source.
        /// </summary>
        public IEnumerable<ButtonDefinition> Buttons => ActualButtons;

        /// <summary>
        /// The root view model for the source, accessible by the data sources to manipulate the tree.
        /// </summary>
        protected TRootViewModel ActualRoot { get; }

        /// <summary>
        /// The modifiable collection of buttons accessible by the data sources.
        /// </summary>
        protected IList<ButtonDefinition> ActualButtons { get; } = new List<ButtonDefinition>();

        public CloudExplorerSourceBase()
        {
            ActualRoot = new TRootViewModel();
            ActualRoot.Initialize();
        }

        /// <summary>
        /// Refreshes the contents of this data source.
        /// </summary>
        public void Refresh()
        {
            ActualRoot.Refresh();
        }

        /// <summary>
        /// Called when the project or current account have changed.
        /// </summary>
        public void InvalidateProjectOrAccount()
        {
            ActualRoot.InvalidateProjectOrAccount();
        }
    }
}
