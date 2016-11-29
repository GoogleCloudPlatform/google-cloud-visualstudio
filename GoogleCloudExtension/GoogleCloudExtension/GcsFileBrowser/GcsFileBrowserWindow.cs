//------------------------------------------------------------------------------
// <copyright file="GcsFileBrowserWindow.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace GoogleCloudExtension.GcsFileBrowser
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Shell.Interop;
    using Utils;

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
    [Guid("374b786d-6f94-4bab-8ca1-74faca9c4f88")]
    public class GcsFileBrowserWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GcsFileBrowserWindow"/> class.
        /// </summary>
        public GcsFileBrowserWindow() : base(null)
        {
            this.Caption = "GcsFileBrowserWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new GcsFileBrowserWindowControl();
        }

        public static void ShowWindow()
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = GoogleCloudExtensionPackage.Instance.FindToolWindow(typeof(GcsFileBrowserWindow), 0, create: true);
            if ((null == window) || (null == window.Frame))
            {
                Debug.WriteLine("Cannot create window.");
                return;
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandlerUtils.HandleExceptions(() => windowFrame.Show());
        }
    }
}
