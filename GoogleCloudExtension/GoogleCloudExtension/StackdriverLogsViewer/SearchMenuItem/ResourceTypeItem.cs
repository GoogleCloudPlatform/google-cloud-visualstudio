using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Collections.ObjectModel;

using GoogleCloudExtension.Utils;
using Google.Apis.Logging.v2.Data.Extensions;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    public class ResourceTypeItem : MenuItemViewModel
    {
        private readonly ResourceKeys _resourceKeys;

        public ResourceTypeItem(ResourceKeys resourceKeys)
        {
            _resourceKeys = resourceKeys;
            Header = _resourceKeys.Type;
            if (_resourceKeys.Keys == null)
            {
                return;
            }

            MenuItems = new ObservableCollection<MenuItemViewModel>();
            MenuItems.Add(new ResourceValueItem("key1"));
            MenuItems.Add(new ResourceValueItem("key2"));
            MenuItems.Add(new ResourceValueItem("key3"));
            MenuItems.Add(new ResourceValueItem("6key1"));
        }
    }
}
