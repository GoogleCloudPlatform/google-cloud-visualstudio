// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceInstanceItem
    {
        private const string Category = "Instance Properties";

        protected Instance Instance { get; }

        public GceInstanceItem(Instance instance)
        {
            Instance = instance;
        }

        [Category(Category)]
        [Description("The name of the instance")]
        public string Name => Instance.Name;

        [Category(Category)]
        [Description("The zone of the instance")]
        public string Zone => Instance.ZoneName();

        [Category(Category)]
        [Description("The machine type for the instance")]
        public string MachineType => Instance.MachineType;

        [Category(Category)]
        [Description("The current status of the instance")]
        public string Status => Instance.Status;

        [Category(Category)]
        [Description("Whether this is an ASP.NET server")]
        public bool IsAspNet => Instance.IsAspnetInstance();

        [Category(Category)]
        [Description("The interna IP address of the instance")]
        public string IpAddress => Instance.GetInternalIpAddress();

        [Category(Category)]
        [Description("The public IP address of the instance")]
        public string PublicIpAddress => Instance.GetPublicIpAddress();

        [Category(Category)]
        [Description("The tags for this instance.")]
        public string Tags => Instance.GetTags();
    }
}