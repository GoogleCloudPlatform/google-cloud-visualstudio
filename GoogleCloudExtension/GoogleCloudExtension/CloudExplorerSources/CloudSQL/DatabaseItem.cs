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
using GoogleCloudExtension.Utils;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.CloudSQL
{
    internal class DatabaseItem : PropertyWindowItemBase
    {
        private const string Category = "Database Properties";

        private readonly Database _database;

        public DatabaseItem(Database database) : base(className: Category, componentName: database.Name)
        {
            _database = database;
        }

        [Category(Category)]
        public string Name => _database.Name;

        [Category(Category)]
        public string Charset => _database.Charset;

        [Category(Category)]
        public string Collation => _database.Collation;

        public override string ToString() => _database.Name;
    }
}
