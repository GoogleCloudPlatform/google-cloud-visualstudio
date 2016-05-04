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
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorer
{
    public abstract class CloudExplorerSourceBase<TRootViewModel> : ICloudExplorerSource where TRootViewModel : SourceRootViewModelBase, new()
    {
        private readonly TRootViewModel _root;
        private readonly IList<ButtonDefinition> _buttons = new List<ButtonDefinition>();

        public TreeHierarchy Root => _root;

        public IEnumerable<ButtonDefinition> Buttons => _buttons;

        public Project CurrentProject { get; set; }

        protected TRootViewModel ActualRoot => _root;

        protected IList<ButtonDefinition> ActualButtons => _buttons;

        public CloudExplorerSourceBase()
        {
            _root = new TRootViewModel();
            _root.Initialize(this);
        }

        public void Refresh()
        {
            _root.Refresh();
        }

        public void InvalidateCredentials()
        {
            _root.InvalidateCredentials();
        }
    }
}
