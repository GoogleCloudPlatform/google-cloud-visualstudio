// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : TreeHierarchy
    {
        public AppEngineRootViewModel()
        {
            // TODO: Set the icon for AppEngine.
            Content = "AppEngine";
            
            // Show the items for the AppEngine hierarchy, to show first the loading message
            // then the items once they are received.
            IsExpanded = true; 
        }
    }
}
