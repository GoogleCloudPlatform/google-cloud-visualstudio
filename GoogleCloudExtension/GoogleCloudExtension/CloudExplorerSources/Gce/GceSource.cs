// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceSource : CloudExplorerSourceBase
    {
        private readonly GceSourceRootViewModel _root = new GceSourceRootViewModel();

        public override TreeHierarchy GetRoot()
        {
            return _root;
        }

        public override void Refresh()
        {
            _root.Refresh();
        }
    }
}
