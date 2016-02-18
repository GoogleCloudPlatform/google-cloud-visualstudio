// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceGaeInstanceItem : GceInstanceItem
    {
        private const string GaeCategory = "AppEngine Properties";

        public GceGaeInstanceItem(GceInstance instance) : base(instance)
        { }

        [Category(GaeCategory)]
        [Description("The AppEngine module for the instance.")]
        public string Module => _instance.GetGaeModule();

        [Category(GaeCategory)]
        [Description("The version of the module for the instance.")]
        public string Version => _instance.GetGaeVersion();
    }
}
