// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

namespace GoogleCloudExtension.CloudExplorer
{
    public interface ICloudExplorerSource
    {
        TreeHierarchy GetRoot();

        void Refresh();
    }
}
