// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class GcsSource : CloudExplorerSourceBase
    {
        private readonly GcsSourceRootViewModel _root = new GcsSourceRootViewModel();

        #region ICloudExplorerSource implementation.

        public override TreeHierarchy GetRoot()
        {
            return _root;
        }

        public override void Refresh()
        {
            _root.Refresh();
        }

        #endregion
    }
}
