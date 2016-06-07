using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class WindowsInstanceItem : GceInstanceItem
    {
        const string Category = "Windows Properties";

        private readonly WindowsInstanceInfo _info;

        public WindowsInstanceItem(Instance instance) : base(instance)
        {
            _info = instance.GetWindowsInstanceInfo();
        }

        [Category(Category)]
        [DisplayName("Windows Version")]
        [Description("The version of Windows installed on this instance.")]
        public string WindowsDisplayName => _info.DisplayName;
    }
}
