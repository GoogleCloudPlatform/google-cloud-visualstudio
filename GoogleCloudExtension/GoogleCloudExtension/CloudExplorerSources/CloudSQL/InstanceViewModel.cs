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

using Google.Apis.SQLAdmin.v1beta4.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.CloudSQL
{
    class InstanceViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        // TODO(talarico): Create an icon for instances.
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/bucket_icon.png";
        private static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = "Loading databases...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = "No databases found."
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = "Failed to list databases.",
            IsError = true
        };

        private readonly CloudSQLSourceRootViewModel _owner;
        private readonly DatabaseInstance _instance;
        private readonly Lazy<InstanceItem> _item;

        public event EventHandler ItemChanged;

        public object Item => _item.Value;

        public InstanceViewModel(CloudSQLSourceRootViewModel owner, DatabaseInstance instance)
        {
            _owner = owner;
            _instance = instance;
            _item = new Lazy<InstanceItem>(GetItem);

            Caption = _instance.Name;
            Icon = s_instanceIcon.Value;

            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                Children.Add(s_loadingPlaceholder);
                var databases = await _owner.DataSource.Value.GetDatabaseListAsync(_instance.Name);
                Children.Clear();
                foreach (var database in databases)
                {
                    Children.Add(new DatabaseViewModel(this, database));
                }
                if (Children.Count == 0)
                {
                    Children.Add(s_noItemsPlacehoder);
                }
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to call api: {ex.Message}");
                Children.Clear();
                Children.Add(s_errorPlaceholder);
            }
        }

        private InstanceItem GetItem() => new InstanceItem(_instance);
    }
}
