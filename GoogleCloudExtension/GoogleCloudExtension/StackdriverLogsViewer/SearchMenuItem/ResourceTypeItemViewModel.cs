// Copyright 2017 Google Inc. All Rights Reserved.
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
    /// <summary>
    /// View model for resource type menu item.
    /// This item represents a single resource type and contains sub menu items for resource key values.
    /// It is tightly coupled with <seealso cref="ResourceTypeMenuViewModel"/> .
    /// </summary>
    public class ResourceTypeItemViewModel : MenuItemViewModel
    {
        private readonly Lazy<LoggingDataSource> _dataSource;

        /// <summary>
        /// The <seealso cref="ResourceTypeKeys"/> object that this menu item represents.
        /// </summary>
        public ResourceKeys ResourceTypeKeys { get; }

        /// <summary>
        /// Choose all submenu item header.
        /// Example:  All instance_id.
        /// </summary>
        public string ChooseAllHeader => GetKeyAt(0) == null ? null : String.Format(Resources.LogsViewerChooseAllMenuHeaderFormat, GetKeyAt(0));

        /// <summary>
        /// Create an instance of <seealso cref="ResourceTypeItemViewModel"/> class.
        /// </summary>
        /// <param name="resourceKeys"><seealso cref="ResourceTypeKeys"/> object.</param>
        /// <param name="dataSource">The logging data source.</param>
        /// <param name="parent">The parent menu item view model object.</param>
        public ResourceTypeItemViewModel(ResourceKeys resourceKeys, Lazy<LoggingDataSource> dataSource, MenuItemViewModel parent) : base(parent)
        {
            _dataSource = dataSource;
            ResourceTypeKeys = resourceKeys;
            Header = ResourceTypeKeys.Type;
            IsSubmenuPopulated = ResourceTypeKeys.Keys?.FirstOrDefault() == null;
            // MenuItem determins the MenuRole of SubmenuItem or SubMenuItemHeader by checking if it has sub menuitems.
            // Adding an invisible item for delay load menu items so that it is recognized as SubMenuItemHeader role.
            // By setting this role, popup is visible. Otherwise popup is invisible.
            if (!IsSubmenuPopulated)
            {
                MenuItems.Add(MenuItemViewModel.InvisibleItem);
            }
        }

        /// <summary>
        /// Gets the key name.
        /// </summary>
        /// <param name="index">Index of the keys list.</param>
        public string GetKeyAt(int index) => ResourceTypeKeys?.Keys?[index];

        /// <summary>
        /// Perform delayed load of sub menu items.
        /// TODO: handle DataSourceException.
        /// </summary>
        protected override async Task LoadSubMenu()
        {
            var values = await _dataSource.Value.ListResourceTypeValuesAsync(ResourceTypeKeys.Type);
            var trimedValues = values?.Select(x => x.Trim(new char[] { '/' })).Where(y => !String.IsNullOrWhiteSpace(y));
            if (trimedValues?.FirstOrDefault() == null)
            {
                return;
            }

            switch (ResourceTypeKeys.Type)
            {
                case ResourceTypeNameConsts.GceInstanceType:
                    await AddGceInstanceSubMenu(trimedValues);
                    break;
                default:
                    foreach (var menuItem in trimedValues.Select(x => new ResourceValueItemViewModel(x, this)))
                    {
                        MenuItems.Add(menuItem);
                    }
                    break;
            }
        }

        private async Task AddGceInstanceSubMenu(IEnumerable<string> instanceIds)
        {
            var dataSource = new GceDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var allInstances = await dataSource.GetInstanceListAsync();
            // Left join instanceIds to allInstances on Id.  
            // Select instance name if id is found in allInstances.
            var menuItems =  
                from id in instanceIds
                join instance in allInstances 
                    on id equals instance.Id?.ToString() into joined
                from subInstance in joined.DefaultIfEmpty()
                orderby subInstance?.Name descending
                select new ResourceValueItemViewModel(id, this, subInstance == null ? id : subInstance.Name);
            menuItems.ToList().ForEach(x => MenuItems.Add(x));
        }
    }
}
