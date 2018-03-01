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

using Google.Apis.Logging.v2.Data;
using Google.Apis.Logging.v2.Data.Extensions;
using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// First item shows first.
        /// </summary>
        private static readonly string[] s_defaultResourceSelections =
            new string[] {
                ResourceTypeNameConsts.GceInstanceType,
                ResourceTypeNameConsts.GaeAppType,
                ResourceTypeNameConsts.GlobalType
            };

        private readonly Func<ILoggingDataSource> _dataSource;
        private MenuItemViewModel _selectedMenuItem;
        private IList<ResourceKeys> _resourceKeys;

        /// <summary>
        /// Gets the selected resource type name.
        /// Example: gae_app,  gce_instance.
        /// </summary>
        public string SelectedTypeNmae => SelectedResourceType?.ResourceTypeKeys?.Type;

        /// <summary>
        /// Gets the selected resource type menu item view model.
        /// </summary>
        public ResourceTypeItemViewModel SelectedResourceType { get; private set; }

        /// <summary>
        /// Gets, sets the selected menu item.
        /// </summary>
        public MenuItemViewModel SelectedMenuItem
        {
            get { return _selectedMenuItem; }
            set { SetValueAndRaise(ref _selectedMenuItem, value); }
        }

        /// <summary>
        /// Initializes an instance of <seealso cref="ResourceTypeMenuViewModel"/> class.
        /// </summary>
        /// <param name="dataSource">Logging data source.</param>
        public ResourceTypeMenuViewModel(Func<ILoggingDataSource> dataSource) : base(null)
        {
            IsSubmenuPopulated = false;
            _dataSource = dataSource;
        }

        /// <summary>
        /// Refers to <seealso cref="LogsViewerViewModel.PopulateResourceTypes"/>
        /// Exception is handled by the caller.
        /// </summary>
        public async Task PopulateResourceTypes()
        {
            if (IsSubmenuPopulated)
            {
                return;
            }

            var descriptors = await _dataSource().GetResourceDescriptorsAsync();
            var newOrderDescriptors = new List<MonitoredResourceDescriptor>();
            // Keep the order.
            foreach (var defaultSelection in s_defaultResourceSelections)
            {
                var desc = descriptors?.FirstOrDefault(x => x.Type == defaultSelection);
                if (desc != null)
                {
                    newOrderDescriptors.Add(desc);
                }
            }

            newOrderDescriptors.AddRange(
                descriptors?.Where(x => !s_defaultResourceSelections.Contains(x.Type)).OrderBy(x => x.DisplayName) ??
                Enumerable.Empty<MonitoredResourceDescriptor>());

            _resourceKeys = await _dataSource().ListResourceKeysAsync();
            var items = _resourceKeys?.Join(
                newOrderDescriptors, keys => keys.Type, desc => desc.Type,
                (keys, desc) => new ResourceTypeItemViewModel(keys, _dataSource, this) { Header = desc.DisplayName });
            foreach (ResourceTypeItemViewModel item in items ?? Enumerable.Empty<ResourceTypeItemViewModel>())
            {
                MenuItems.Add(item);
            }

            if (MenuItems.Count != 0)
            {
                CommandBubblingHandler(MenuItems.FirstOrDefault());
                IsSubmenuPopulated = true;
            }
        }

        /// <summary>
        /// Handle menu item selection event.
        /// </summary>
        /// <param name="originalSource">The original selected menu item.</param>
        protected override void CommandBubblingHandler(MenuItemViewModel originalSource)
        {
            if (originalSource == null || _selectedMenuItem == originalSource)
            {
                return;
            }

            StringBuilder selected = new StringBuilder();
            var menuItem = originalSource;
            while (menuItem != this)
            {
                selected.Insert(0, menuItem.Header);
                if (menuItem.MenuItemParent != this)
                {
                    selected.Insert(0, ".");
                }
                else
                {
                    // Every direct children is ResourceTypeItem type.
                    SelectedResourceType = menuItem as ResourceTypeItemViewModel;
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
