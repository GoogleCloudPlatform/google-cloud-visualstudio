// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.AppEngineApps
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("fe34c2aa-59b3-40ad-a3b6-2743d072d2aa")]
    public class AppEngineAppsToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppEngineAppsToolWindow"/> class.
        /// </summary>
        public AppEngineAppsToolWindow() : base(null)
        {
            this.Caption = "Google AppEngine";
            var model = new AppEngineAppsToolViewModel();
            var content = new AppEngineAppsToolWindowControl { DataContext = model };
            this.Content = content;

            // Load the list of apps for the current user and project.
            model.LoadAppEngineAppListAsync();

            ExtensionAnalytics.ReportWindowOpened(nameof(AppEngineAppsToolWindow));
        }
    }
}
