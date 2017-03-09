// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace GoogleCloudExtension.StackdriverErrorReporting
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
    [Guid("f62a2c47-c030-456d-8a45-8e882fcb0ee3")]
    public class ErrorReportingDetailToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Gets the view model of the user control <seealso cref="ErrorReportingDetailToolWindowControl"/>.
        /// </summary>
        public ErrorReportingDetailViewModel ViewModel { get; }

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
            ViewModel = new ErrorReportingDetailViewModel();
            (this.Content as ErrorReportingDetailToolWindowControl).DataContext = ViewModel;
        }
    }
}
