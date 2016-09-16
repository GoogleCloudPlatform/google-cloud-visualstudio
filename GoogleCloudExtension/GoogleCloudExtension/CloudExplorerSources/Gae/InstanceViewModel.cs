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

using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using System;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE instance in the Google Cloud Explorer Window.
    /// </summary>
    class InstanceViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private readonly VersionViewModel _owner;

        private readonly Instance _instance;

        public event EventHandler ItemChanged;

        public object Item => GetItem();

        public InstanceViewModel(VersionViewModel owner, Instance instance)
        {
            _owner = owner;
            _instance = instance;

            Caption = _instance.VmName;
        }

        public InstanceItem GetItem() => new InstanceItem(_instance);
    }
}
