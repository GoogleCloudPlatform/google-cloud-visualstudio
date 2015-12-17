// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
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
        private readonly AppEngineAppsToolViewModel _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppEngineAppsToolWindow"/> class.
        /// </summary>
        public AppEngineAppsToolWindow() : base(null)
        {
            this.Caption = "Google AppEngine";
            _model = new AppEngineAppsToolViewModel();
            var content = new AppEngineAppsToolWindowControl { DataContext = _model };
            this.Content = content;

            // Load the list of apps for the current user and project.
            _model.LoadAppEngineAppListAsync();

            // Add an event handler to the content so we can listen for selection changes.
            content.SelectedItemChanged += Content_SelectedItemChanged;
        }

        private void Content_SelectedItemChanged(object sender, ModuleAndVersion e)
        {
            _model.CurrentApp = e;
        }
    }
}
