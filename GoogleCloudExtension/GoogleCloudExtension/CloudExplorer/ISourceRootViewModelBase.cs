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

using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    public interface ISourceRootViewModelBase : ITreeHierarchy
    {
        /// <summary>
        /// Returns whether this view model is busy loading data.
        /// </summary>
        bool IsLoadingState { get; }

        /// <summary>
        /// Returns whether this view model is already loaded with data.
        /// </summary>
        bool IsLoadedState { get; }

        /// <summary>
        /// Returns the icon to use for the root for this data source. By default all sources use the
        /// default <seealso cref="Theming.CommonImageResources.s_logoIcon"/>.
        /// Each node can override the icon with a custom one if desired.
        /// </summary>
        ImageSource RootIcon { get; }

        /// <summary>
        /// Returns the caption to use for the root node for this data source.
        /// </summary>
        string RootCaption { get; }

        /// <summary>
        /// Returns the tree node to use when there's an error loading data.
        /// </summary>
        TreeLeaf ErrorPlaceholder { get; }

        /// <summary>
        /// Returns the tree node to use when there's no data returned by this data source.
        /// </summary>
        TreeLeaf NoItemsPlaceholder { get; }

        /// <summary>
        /// Returns the tree node to use while loading data.
        /// </summary>
        TreeLeaf LoadingPlaceholder { get; }

        /// <summary>
        /// Returns the context in which this source root view model is working.
        /// </summary>
        ICloudSourceContext Context { get; }

        void Initialize(ICloudSourceContext context);
        void Refresh();
        void InvalidateProjectOrAccount();
    }
}