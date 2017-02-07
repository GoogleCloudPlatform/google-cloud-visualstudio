using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Collections.ObjectModel;
using System.Linq;
using GoogleCloudExtension.Utils;
using Google.Apis.Logging.v2.Data.Extensions;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    public class ResourceTypeItem : MenuItemViewModel
    {
        private LogsViewerViewModel LogsViewerModel => MenuItemParent as LogsViewerViewModel;

        public ResourceKeys ResourceTypeKeys { get; }

        public ResourceTypeItem(ResourceKeys resourceKeys, IMenuItem parent) : base(parent)
        {
            ResourceTypeKeys = resourceKeys;
            Header = ResourceTypeKeys.Type;
            IsSubmenuPopulated = ResourceTypeKeys.Keys?.FirstOrDefault() == null;
            // The resource type contains keys, add a fake item to make it submenuheader Role.
            if (!IsSubmenuPopulated)
            {
                MenuItems.Add(MenuItemViewModel.CreateFakeItem());
            }
        }

        protected override async Task LoadSubMenu()
        {
            // TODO: remove before checkin. Test only.
            await System.Threading.Tasks.Task.Delay(2000);

            var keys = await LogsViewerModel.GetResourceValues(ResourceTypeKeys);
            if (keys == null)
            {
                return;
            }

            foreach (var menuItem in keys.Select(x => new ResourceValueItem(x, this)))
            {
                MenuItems.Add(menuItem);
            }
        }
    }
}
