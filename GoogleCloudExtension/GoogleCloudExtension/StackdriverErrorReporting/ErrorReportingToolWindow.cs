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

using GoogleCloudExtension.Accounts;
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
    [Guid("4b3c62b4-2121-40a1-8cd5-8f794760b35e")]
    public class ErrorReportingToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Gets a <seealso cref="ErrorReportingViewModel"/> object that is associated with the Window.
        /// </summary>
        public ErrorReportingViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReportingToolWindow"/> class.
        /// </summary>
        public ErrorReportingToolWindow() : base(null)
        {
            this.Caption = Resources.ErrorReportingToolWindowCaption;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ErrorReportingToolWindowControl();

            ViewModel = new ErrorReportingViewModel();
            (Content as ErrorReportingToolWindowControl).DataContext = ViewModel;
        }
    }
}