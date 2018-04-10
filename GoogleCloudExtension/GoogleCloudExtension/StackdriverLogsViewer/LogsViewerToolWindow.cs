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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

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
        public virtual ILogsViewerViewModel ViewModel => _content.ViewModel;

        private LogsViewerToolWindowControl _content;

        /// <summary>Gets or sets the content of this tool window. </summary>
        /// <returns>The object that represents the content of this tool window.</returns>
        public override object Content
        {
            get { return _content; }
            set { _content = value as LogsViewerToolWindowControl; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsViewerToolWindow"/> class.
        /// </summary>
        public LogsViewerToolWindow()
        {
            Caption = Resources.LogViewerToolWindowCaption;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            _content = new LogsViewerToolWindowControl();

            CredentialsStore.Default.CurrentProjectIdChanged += (sender, e) => CreateNewViewModel();
            CredentialsStore.Default.Reset += (sender, e) => CreateNewViewModel();

            EventsReporterWrapper.ReportEvent(LogsViewerOpenEvent.Create());
        }

        /// <summary>
        /// Create view mode when window object is created.
        /// </summary>
        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            CreateNewViewModel();
        }

        protected override void OnClose()
        {
            base.OnClose();
            ViewModel?.Dispose();
        }

        private void CreateNewViewModel()
        {
            object toolWindowIdNumber;
            ((IVsWindowFrame)Frame).GetProperty((int)VsFramePropID.MultiInstanceToolNum, out toolWindowIdNumber);
            int windowIdNumber = Convert.ToInt32(toolWindowIdNumber);
            var newModel = new LogsViewerViewModel(windowIdNumber);
            _content.DataContext = newModel;
            newModel.InvalidateAllProperties();
        }
    }
}
