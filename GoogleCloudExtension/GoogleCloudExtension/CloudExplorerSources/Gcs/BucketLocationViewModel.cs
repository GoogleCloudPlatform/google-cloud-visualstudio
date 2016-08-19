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

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    /// <summary>
    /// This class is the view model for the location of a bucket, showing the buckets
    /// under this location and the right icon.
    /// </summary>
    internal class BucketLocationViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/zone_icon.png";

        private static readonly Lazy<ImageSource> s_zoneIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly GcsSourceRootViewModel _owner;
        private IEnumerable<BucketViewModel> _buckets;

        public BucketLocationViewModel(GcsSourceRootViewModel owner, string name, IEnumerable<BucketViewModel> buckets)
        {
            _owner = owner;
            _buckets = buckets;

            Caption = $"{name} ({buckets.Count()})";
            Icon = s_zoneIcon.Value;
            foreach (var bucket in _buckets)
            {
                Children.Add(bucket);
            }
        }
    }
}
