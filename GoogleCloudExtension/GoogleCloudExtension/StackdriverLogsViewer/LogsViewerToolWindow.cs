// Copyright 2016 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.StackdriverLogsViewer
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
    [Guid("043c1f77-7cbf-4676-86c3-f205ed506d26")]
    public class LogsViewerToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Gets a <seealso cref="LogsViewerViewModel"/> object that is associated with the Logs Viewer Window.
        /// </summary>
        public LogsViewerViewModel ViewModel => (Content as LogsViewerToolWindowControl)?.DataContext as LogsViewerViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsViewerToolWindow"/> class.
        /// </summary>
        public LogsViewerToolWindow() : base(null)
        {
            Caption = Resources.LogViewerToolWindowCaption;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new LogsViewerToolWindowControl();

            CredentialsStore.Default.CurrentProjectIdChanged += (sender, e) => CreateNewViewModel();
            CredentialsStore.Default.Reset += (sender, e) => CreateNewViewModel();
        }

        /// <summary>
        /// Create view mode when window object is created. 
        /// </summary>
        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            CreateNewViewModel();
        }

        private void CreateNewViewModel()
        {
            var control = Content as LogsViewerToolWindowControl;
            var newModel = new LogsViewerViewModel();
            control.DataContext = newModel;
            newModel.InvalidateAllProperties();
        }
    }
}
