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


namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This is the base class for all sources of data for the Cloud Explorer. Implements all of the basic
    /// behaviors, such as loading data, refreshing the data, etc...
    /// </summary>
    /// <typeparam name="TRootViewModel">The type of the root view model for the data source.</typeparam>
    public abstract class DynamicCloudExplorerSourceBase<TRootViewModel> : CloudExplorerSourceBase<TRootViewModel>
        where TRootViewModel : SourceRootViewModelBase, new()
    {
        /// <summary>
        /// The root of the hierarchy for this data source.
        /// </summary>
        public sealed override TRootViewModel Root { get; }

        protected DynamicCloudExplorerSourceBase(ICloudSourceContext context) : base(context)
        {
            Root = new TRootViewModel();
            Root.Initialize(context);
        }
    }
}
