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
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers for ToolWindow
    /// </summary>
    internal static class ToolWindowCommandUtils
    {
        /// <summary>
        /// Shows the tool window
        /// </summary>
        /// <returns>The tool window object if it is found.</returns>
        public static TToolWindow ShowToolWindow<TToolWindow>() where TToolWindow : ToolWindowPane
        {
            if (GoogleCloudExtensionPackage.Instance == null)
            {
                Debug.WriteLine("GoogleCloudExtensionPackage.Instance is null, unexpected error");
                return null;
            }

            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = GoogleCloudExtensionPackage.Instance.FindToolWindow(typeof(TToolWindow), 0, true);
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                if (window?.Frame == null)
                {
                    throw new NotSupportedException("Failed to create the tool window");
                }

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            });
            return window as TToolWindow;
        }

        /// <summary>
        /// Response to <seealso cref="OleMenuCommand.BeforeQueryStatus"/> 
        /// to enable menu item if current project id is valid.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public static void EnableMenuItemOnValidProjectId(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }
            menuCommand.Enabled = !String.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentProjectId);
        }
    }
}