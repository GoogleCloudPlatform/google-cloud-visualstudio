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

using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.PlatformUI;

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// Base calls for all dialogs in the extension, ensures consistent appearence for all dialogs.
    /// </summary>
    public class CommonDialogWindowBase : DialogWindow
    {
        public CommonDialogWindowBase(string title, double width, double height) : this(title)
        {
            Width = width;
            Height = height;
        }

        public CommonDialogWindowBase(string title) : this()
        {
            Title = title;
        }

        private CommonDialogWindowBase()
        {
            // Common settings to all dialogs.
            ResizeMode = System.Windows.ResizeMode.NoResize;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;

            // Report that this window is opened.
            ExtensionAnalytics.ReportScreenView(this);
        }
    }
}
