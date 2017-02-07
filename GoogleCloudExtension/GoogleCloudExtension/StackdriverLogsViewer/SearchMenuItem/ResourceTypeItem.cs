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

using Google.Apis.Logging.v2.Data.Extensions;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    public class ResourceTypeItem : MenuItemViewModel
    {
        private ResourceTypeMenuViewModel _resourceTypeViewModel => MenuItemParent as ResourceTypeMenuViewModel;

        public ResourceKeys ResourceTypeKeys { get; }

        public string ChooseAllHeader => GetKeysAt(0) == null ? null : String.Format(Resources.LogsViewerChooseAllMenuHeaderFormat, GetKeysAt(0));

        public ResourceTypeItem(ResourceKeys resourceKeys, MenuItemViewModel parent) : base(parent)
        {
            ResourceTypeKeys = resourceKeys;
            Header = ResourceTypeKeys.Type;
            IsSubmenuPopulated = ResourceTypeKeys.Keys?.FirstOrDefault() == null;
            // The resource type contains keys, add a fake item to make it submenuheader Role.
            if (!IsSubmenuPopulated)
            {
                MenuItems.Add(MenuItemViewModel.FakeItem);
            }
        }

        public string GetKeysAt(int index)
        {
            return ResourceTypeKeys?.Keys?[index];
        }

        protected override async Task LoadSubMenu()
        {
            var keys = await _resourceTypeViewModel.GetResourceValues(ResourceTypeKeys);
            if (keys == null)
            {
                return;
            }

            IEnumerable<Tuple<string, string>> headers = keys.Select(x => new Tuple<string, string>(x, x));
            if (ResourceTypeKeys.Type == "gce_instance")
            {
                headers = await GceInstanceIdToName(keys);
            }

            foreach (var menuItem in headers.Select(x => new ResourceValueItem(x.Item1, this, x.Item2)))
            {
                MenuItems.Add(menuItem);
            }
        }

        private async Task<IEnumerable<Tuple<string, string>>> GceInstanceIdToName(IEnumerable<string> instanceIds)
        {
            var dataSource = new GceDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var allInstances = await dataSource.GetInstanceListAsync();
            return from id in instanceIds
                   join instance in allInstances on id equals instance.Id?.ToString() into joined
                   from subInstance in joined.DefaultIfEmpty()
                   orderby subInstance?.Name descending
                   select new Tuple<string, string>(id,  subInstance == null ? id : subInstance.Name);
        }
    }
}
