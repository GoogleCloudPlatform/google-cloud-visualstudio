// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using System.ComponentModel;

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

        [Description("The amount of traffic assigned to this version")]
        [Category("AppEngine Properties")]
        public string TrafficSplit => string.Format("{0:0.00}%", _target.TrafficSplit * 100.0);

        [Description("The version name")]
        [Category("AppEngine Properties")]
        public string Version => _target.Version;

        #endregion
    }
}
