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
using System.Linq;

namespace GoogleCloudExtension.CloudExplorerSources.CloudSQL
{
    /// <summary>
    /// This class represents a Cloud SQL instance (MySQL instance) in the Properties Window.
    /// </summary>
    internal class InstanceItem : PropertyWindowItemBase
    {
        private readonly DatabaseInstance _instance;

        public InstanceItem(DatabaseInstance instance) : base(className: Resources.CloudExplorerSqlInstanceCategory, componentName: instance.Name)
        {
            _instance = instance;
        }

        [LocalizedCategory(nameof(Resources.CloudExplorerSqlInstanceCategory))]
        public string Name => _instance.Name;

        [LocalizedCategory(nameof(Resources.CloudExplorerSqlInstanceCategory))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerSqlBackendTypeDisplayName))]
        public string BackendType => _instance.BackendType;

        [LocalizedCategory(nameof(Resources.CloudExplorerSqlInstanceCategory))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerSqlDatabaseVersionDisplayName))]
        public string DatabaseVersion => _instance.DatabaseVersion;

        [LocalizedCategory(nameof(Resources.CloudExplorerSqlInstanceCategory))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerSqlInstanceTypeDisplayName))]
        public string InstanceType => _instance.InstanceType;

        // TODO(talarico): Support multiple IP  addresses.
        [LocalizedCategory(nameof(Resources.CloudExplorerSqlInstanceCategory))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerSqlInstanceIpAddressDisplayName))]
        public string IpAddress => _instance.IpAddresses?.First().IpAddress;

        [LocalizedCategory(nameof(Resources.CloudExplorerSqlInstanceCategory))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerSqlInstanceIp6AddressDisplayName))]
        public string Ipv6Address => _instance.Ipv6Address;

        [LocalizedCategory(nameof(Resources.CloudExplorerSqlInstanceCategory))]
        public string State => _instance.State;

        public override string ToString() => _instance.Name;
    }
}
