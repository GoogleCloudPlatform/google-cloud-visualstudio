using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool _hasLoadedValues;
        public ResourceKeys ResourceTypeKeys { get; }

        public ResourceTypeItem(ResourceKeys resourceKeys, IMenuItem parent) : base(parent)
        {
            ResourceTypeKeys = resourceKeys;
            Header = ResourceTypeKeys.Type;
            LoadSubMenuItem();
        }

        protected override void Execute()
        {
            if (_hasLoadedValues)
            {
                base.Execute();
            }
        }

        private LogsViewerViewModel LogsViewerModel => MenuItemParent as LogsViewerViewModel;

        private void LoadSubMenuItem()
        {
            _hasLoadedValues = true;
            if (ResourceTypeKeys.Keys != null && ResourceTypeKeys.Keys.Count > 0)
            {
                var values = LogsViewerModel.GetResourceValues(ResourceTypeKeys);
                if (values != null)
                {
                    foreach (var menuItem in values.Select(x => new ResourceValueItem(x, this)))
                    {
                        MenuItems.Add(menuItem);
                    }
                }
            }
        }
    }
}
