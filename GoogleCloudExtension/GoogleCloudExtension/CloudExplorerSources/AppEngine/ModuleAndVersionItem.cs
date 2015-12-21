using GoogleCloudExtension.GCloud.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    /// <summary>
    /// This class provides read-only access to the properties in the ModuleAndVersion
    /// class. Together with the documentation for those properties.
    /// </summary>
    internal class ModuleAndVersionItem
    {
        private readonly ModuleAndVersion _target;

        internal ModuleAndVersionItem(ModuleAndVersion target)
        {
            _target = target;
        }

        #region Properties to be displayed in the properties window

        [Description("The name of the module")]
        [Category("AppEngine Properties")]
        public string Module => _target.Module;

        [Description("Whether the version is the default version")]
        [Category("AppEngine Properties")]
        public bool IsDefault => _target.IsDefault;

        [Description("The version name")]
        [Category("AppEngine Properties")]
        public string Version => _target.Version;

        #endregion
    }
}
