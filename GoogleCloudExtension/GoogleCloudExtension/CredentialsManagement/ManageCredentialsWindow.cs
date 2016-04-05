// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.CredentialsManagement
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
    [Guid("170d091f-5a05-46e9-9d7b-3fdab8b413d3")]
    public class ManageCredentialsWindow : DialogWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManageCredentialsWindow"/> class.
        /// </summary>
        public ManageCredentialsWindow() : base(null)
        {
            Title = "Choose Users";
            var model = new ManageCredentialsViewModel();
            Content = new ManageUsersWindowControl { DataContext = model };
        }
    }
}
