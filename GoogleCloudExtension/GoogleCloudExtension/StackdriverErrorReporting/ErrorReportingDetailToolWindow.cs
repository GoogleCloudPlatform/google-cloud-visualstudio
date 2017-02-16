//------------------------------------------------------------------------------
// <copyright file="ErrorReportingDetailToolWindow.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

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
    [Guid("f62a2c47-c030-456d-8a45-8e882fcb0ee3")]
    public class ErrorReportingDetailToolWindow : ToolWindowPane
    {
        public ErrorReportingDetailViewModel ViewModel
        {
            get
            {
                return (Content as ErrorReportingDetailToolWindowControl)?.ViewModel;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReportingDetailToolWindow"/> class.
        /// </summary>
        public ErrorReportingDetailToolWindow() : base(null)
        {
            this.Caption = Resources.ErrorReportingDetailToolWindowCaption;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ErrorReportingDetailToolWindowControl();
        }
    }
}
