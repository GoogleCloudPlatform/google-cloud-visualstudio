// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/app_engine.png";
        static readonly Lazy<ImageSource> s_icon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        public AppEngineRootViewModel()
        {
            // TODO: Set the icon for AppEngine.
            Content = "AppEngine";
            Icon = s_icon.Value;
            
            // Show the items for the AppEngine hierarchy, to show first the loading message
            // then the items once they are received.
            IsExpanded = true; 
        }
    }
}
