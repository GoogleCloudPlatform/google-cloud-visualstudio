// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;

namespace GoogleCloudExtension.CloudExplorer
{
    public interface ICloudExplorerItemSource
    {
        object Item { get; }

        event EventHandler ItemChanged;
    }
}
