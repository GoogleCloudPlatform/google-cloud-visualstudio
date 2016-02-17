// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceSource : ICloudExplorerSource
    {
        private readonly GceSourceRootViewModel _root = new GceSourceRootViewModel();

        public TreeHierarchy GetRoot()
        {
            return _root;
        }

        public void Refresh()
        {
            _root.Refresh();
        }
    }
}
