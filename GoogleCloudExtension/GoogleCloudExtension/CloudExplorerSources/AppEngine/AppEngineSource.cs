// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineSource : ICloudExplorerSource
    {
        private readonly AppEngineRootViewModel _root = new AppEngineRootViewModel();

        #region ICloudExplorerSource

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
