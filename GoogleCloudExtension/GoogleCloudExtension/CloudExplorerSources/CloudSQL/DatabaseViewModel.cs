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
using GoogleCloudExtension.Utils;
using System;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.CloudSQL
{
    class DatabaseViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        // TODO(talarico): Create an icon for databases.
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/bucket_icon.png";
        private static readonly Lazy<ImageSource> s_databaseIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly InstanceViewModel _owner;
        private readonly Database _database;
        private readonly Lazy<DatabaseItem> _item;

        public event EventHandler ItemChanged;

        public object Item => _item.Value;

        public DatabaseViewModel(InstanceViewModel owner, Database database)
        {
            _owner = owner;
            _database = database;
            _item = new Lazy<DatabaseItem>(GetItem);

            Caption = _database.Name;
            Icon = s_databaseIcon.Value;
        }

        private DatabaseItem GetItem() => new DatabaseItem(_database);
    }
}
