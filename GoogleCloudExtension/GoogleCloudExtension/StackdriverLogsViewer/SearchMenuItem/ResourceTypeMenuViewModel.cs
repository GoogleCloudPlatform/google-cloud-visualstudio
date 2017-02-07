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

using Google.Apis.Logging.v2.Data;
using Google.Apis.Logging.v2.Data.Extensions;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// The class is the view model for Resource Type, resource key value selector.
    /// It is tightly coupled with <seealso cref="LogsViewerViewModel"/>. 
    /// </summary>
    public class ResourceTypeMenuViewModel : MenuItemViewModel
    {
        /// <summary>
        /// The default resource types to show. 
        /// Order matters, if a resource type in the list does not have logs, fall back to the next one. 
        /// </summary>
        private static readonly string[] s_defaultResourceSelections =
            new string[] {
                "gce_instance",
                "gae_app",
                "global"
            };

        public string SelectedTypeNmae => SelectedResourceType?.ResourceTypeKeys?.Type;
        private readonly Lazy<LoggingDataSource> _dataSource;
        public bool Populated { get; private set; }


        private IList<ResourceKeys> _resourceKeys;
        private IList<MonitoredResourceDescriptor> _resourceDescriptors;

        private MenuItemViewModel _selectedMenuItem;
        public ResourceTypeItem SelectedResourceType { get; private set; }
        private ObservableCollection<MenuItemViewModel> _resourceKeysCollection =
            new ObservableCollection<MenuItemViewModel>(new MenuItemViewModel[] { MenuItemViewModel.FakeItem });

        public MenuItemViewModel SelectedMenuItem
        {
            get { return _selectedMenuItem; }
            set { SetValueAndRaise(ref _selectedMenuItem, value); }
        }

        public ResourceTypeMenuViewModel(Lazy<LoggingDataSource> dataSource) : base(null)
        {
            _dataSource = dataSource;
            Populated = false;
        }

        public async Task PopulateResourceTypes()
        {
            if (Populated)
            {
                return;
            }

            _resourceKeys = await _dataSource.Value.ListResourceKeysAsync();

            var all = await _dataSource.Value.GetResourceDescriptorsAsync();
            var descriptors = all.Where(x => _resourceKeys.Any(item => item?.Type == x.Type));
            List<MonitoredResourceDescriptor> newOrderDescriptors = new List<MonitoredResourceDescriptor>();
            // Keep the order.
            foreach (var defaultSelection in s_defaultResourceSelections)
            {
                var desc = descriptors?.FirstOrDefault(x => x.Type == defaultSelection);
                if (desc != null)
                {
                    newOrderDescriptors.Add(desc);
                }
            }
            newOrderDescriptors.AddRange(descriptors.Where(x => !s_defaultResourceSelections.Contains(x.Type)));
            _resourceDescriptors = newOrderDescriptors;  // This will set the selected item to first element.

            foreach(var item in _resourceDescriptors.Select(
                x => new ResourceTypeItem(_resourceKeys.FirstOrDefault(item => item.Type == x.Type), this)))
            {
                MenuItems.Add(item);
            }

            if (MenuItems.Count != 0)
            {
                CommandBubblingHandler(MenuItems.FirstOrDefault());
                Populated = true;
            }
        }

        public async Task<IEnumerable<string>> GetResourceValues(ResourceKeys resourceKeys)
        {
            try
            {
                var values = await _dataSource.Value.ListResourceTypeValuesAsync(resourceKeys.Type, null);
                return values?.Select(x => x.Trim(new char[] { '/' }));
            }
            catch (DataSourceException ex)
            {
                throw;
            }
        }

        protected override void CommandBubblingHandler(MenuItemViewModel originalSource)
        {
            if (originalSource == null || _selectedMenuItem == originalSource)
            {
                return;
            }

            var menuItem = originalSource;

            StringBuilder selected = new StringBuilder();
            while (menuItem != this)
            {
                selected.Insert(0, menuItem.Header);
                if (menuItem.MenuItemParent != this)
                {
                    selected.Insert(0, ".");
                }
                else
                {
                    // The direct children are of type ResourceTypeItem.
                    SelectedResourceType = menuItem as ResourceTypeItem;
                }
                menuItem = menuItem.MenuItemParent;
            }

            Header = selected.ToString();

            // Note, parent LogsViewerViewModel subscribes to PropertyChanged event.
            // So, this triggers OnPropertyChanged event in parent LogsViewerViewModel.
            SelectedMenuItem = originalSource;
        }
    }
}
