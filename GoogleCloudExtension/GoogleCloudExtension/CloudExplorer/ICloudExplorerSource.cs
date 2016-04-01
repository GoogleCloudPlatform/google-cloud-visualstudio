// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorer
{
    public interface ICloudExplorerSource
    {
        TreeHierarchy Root { get; }

        IEnumerable<ButtonDefinition> Buttons { get; }

        GcpProject CurrentProject { get; set; }

        void Refresh();
    }
}
