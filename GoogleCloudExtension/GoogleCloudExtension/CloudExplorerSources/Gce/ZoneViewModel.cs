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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class ZoneViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/zone_icon.png";
        private static readonly Lazy<ImageSource> s_zoneIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly GceSourceRootViewModel _owner;

        public ZoneViewModel(GceSourceRootViewModel owner, string zoneName, IEnumerable<Instance> instances)
        {
            _owner = owner;

            Caption = zoneName;
            Icon = s_zoneIcon.Value;

            var viewModels = instances.Select(x => new GceInstanceViewModel(owner, x));
            foreach (var viewModel in viewModels)
            {
                Children.Add(viewModel);
            }
        }
    }
}