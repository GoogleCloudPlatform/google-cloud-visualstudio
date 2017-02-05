using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    public class ResourceValueItem : MenuItemViewModel
    {
        public ResourceValueItem(string resourceKey)
        {
            Header = resourceKey;
        }
    }
}
