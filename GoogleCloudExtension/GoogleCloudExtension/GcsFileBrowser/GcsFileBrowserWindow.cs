﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using Google.Apis.Storage.v1.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.GcsFileBrowser
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
    [Guid("374b786d-6f94-4bab-8ca1-74faca9c4f88")]
    public class GcsFileBrowserWindow : ToolWindowPane
    {
        private static int s_nextWindowId = 0;

        private GcsBrowserViewModel ViewModel { get; }

        private GcsFileBrowserWindowControl BrowserContent => (GcsFileBrowserWindowControl)Content;

        /// <summary>
        /// Initializes a new instance of the <see cref="GcsFileBrowserWindow"/> class.
        /// </summary>
        public GcsFileBrowserWindow() : base(null)
        {
            ViewModel = new GcsBrowserViewModel(this);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new GcsFileBrowserWindowControl { DataContext = ViewModel };
        }

        public static void BrowseBucket(Bucket bucket)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = GoogleCloudExtensionPackage.Instance.FindToolWindow(
                typeof(GcsFileBrowserWindow),
                s_nextWindowId,
                create: false);
            if (window == null)
            {
                window = GoogleCloudExtensionPackage.Instance.FindToolWindow(
                    typeof(GcsFileBrowserWindow),
                    s_nextWindowId,
                    create: true);
                if (window == null || window.Frame == null)
                {
                    Debug.WriteLine($"Could not create window with id {s_nextWindowId}");
                    return;
                }
            }
            s_nextWindowId += 1;

            var browserWindow = window as GcsFileBrowserWindow;
            if (browserWindow == null)
            {
                Debug.WriteLine($"Invalid window found {window.GetType().FullName}");
                return;
            }
            browserWindow.UpdateBucket(bucket);

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            windowFrame.Show();
        }

        internal void SelectAllRows()
        {
            BrowserContent.SelectAllRows();
        }

        private void UpdateBucket(Bucket bucket)
        {
            ViewModel.Bucket = bucket;
            Caption = bucket.Name;
        }
    }
}
