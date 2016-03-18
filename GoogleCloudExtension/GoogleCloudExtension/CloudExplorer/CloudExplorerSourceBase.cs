// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.CloudExplorer
{
    public abstract class CloudExplorerSourceBase : ICloudExplorerSource
    {
        public abstract TreeHierarchy GetRoot();

        public virtual IEnumerable<ButtonDefinition> GetButtons() => Enumerable.Empty<ButtonDefinition>();

        public abstract void Refresh();
    }
}
