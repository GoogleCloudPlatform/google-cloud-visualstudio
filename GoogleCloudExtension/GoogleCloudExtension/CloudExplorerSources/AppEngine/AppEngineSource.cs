// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineSource : CloudExplorerSourceBase
    {
        private readonly AppEngineRootViewModel _root = new AppEngineRootViewModel();

        #region ICloudExplorerSource

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
