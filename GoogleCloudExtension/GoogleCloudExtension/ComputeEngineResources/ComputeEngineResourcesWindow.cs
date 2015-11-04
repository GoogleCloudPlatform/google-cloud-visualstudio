// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GCloud;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.ComputeEngineResources
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
    [Guid("9dbd3f5a-d310-4c8e-9072-3e85f12e1ae2")]
    public class ComputeEngineResourcesWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeEngineResourcesWindow"/> class.
        /// </summary>
        public ComputeEngineResourcesWindow() : base(null)
        {
            this.Caption = "Google Compute Engine Resources";

            var model = new ComputeEngineResourcesViewModel();
            var content = new ComputeEngineResourcesWindowControl { DataContext = model };
            this.Content = content;

            // Load the model asynchronously.
            model.LoadComputeInstancesList();
        }
    }
}
