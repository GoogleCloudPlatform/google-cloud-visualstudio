// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class GcsSource : ICloudExplorerSource
    {
        private readonly GcsSourceRootViewModel _root = new GcsSourceRootViewModel();

        #region ICloudExplorerSource implementation.

        public TreeHierarchy GetRoot()
        {
            return _root;
        }

        public void Refresh()
        {
            _root.Refresh();
        }

        #endregion
    }
}
