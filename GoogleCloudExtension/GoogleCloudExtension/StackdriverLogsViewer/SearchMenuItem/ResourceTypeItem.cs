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
        private bool HasPopulatedSubMenus;
        public ResourceKeys ResourceTypeKeys { get; }

        public ResourceTypeItem(ResourceKeys resourceKeys, IMenuItem parent) : base(parent)
        {
            ResourceTypeKeys = resourceKeys;
            Header = ResourceTypeKeys.Type;
            //LoadSubMenuItem();
        }

        //protected override void Execute()
        //{
        //    if (HasPopulatedSubMenus)
        //    {
        //        base.Execute();
        //    }
        //}

        private LogsViewerViewModel LogsViewerModel => MenuItemParent as LogsViewerViewModel;

        private void LoadSubMenuItems(IEnumerable<string> resourceKeyValues)
        {
            if (HasPopulatedSubMenus)
            {
                Debug.WriteLine("LoadSubMenuItems has already been called before, skip.");
                return;
            }
            HasPopulatedSubMenus = true;
            if (resourceKeyValues == null) {
                return;
            }
            foreach (var menuItem in resourceKeyValues.Select(x => new ResourceValueItem(x, this)))
            {
                MenuItems.Add(menuItem);
            }
        }
    }
}
