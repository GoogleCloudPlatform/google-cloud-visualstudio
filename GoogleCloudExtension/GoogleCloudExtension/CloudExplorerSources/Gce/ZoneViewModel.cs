// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class ZoneViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/zone_icon.png";
        private static readonly Lazy<ImageSource> s_zoneIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly GceSourceRootViewModel _owner;

        public ZoneViewModel(GceSourceRootViewModel owner, string zoneName, IEnumerable<Instance> instances)
        {
            _owner = owner;

            Content = zoneName;
            Icon = s_zoneIcon.Value;

            var viewModels = instances.Select(x => new GceInstanceViewModel(owner, x));
            foreach (var viewModel in viewModels)
            {
                Children.Add(viewModel);
            }
        }
    }
}