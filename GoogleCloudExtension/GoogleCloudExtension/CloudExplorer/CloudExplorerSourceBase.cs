// Copyright 2018 Google Inc. All Rights Reserved.
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
    public abstract class CloudExplorerSourceBase<TRootViewModel> : ICloudExplorerSource<TRootViewModel>
        where TRootViewModel : ISourceRootViewModelBase
    {
        /// <summary>
        /// The root of the hierarchy for this data source.
        /// </summary>
        public abstract TRootViewModel Root { get; }

        /// <summary>
        /// The context in which this source is being used.
        /// </summary>
        public ICloudSourceContext Context { get; }

        protected CloudExplorerSourceBase(ICloudSourceContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Refreshes the contents of this data source.
        /// </summary>
        public void Refresh() => Root.Refresh();

        /// <summary>
        /// Called when the project or current account have changed.
        /// </summary>
        public void InvalidateProjectOrAccount() => Root.InvalidateProjectOrAccount();
    }
}