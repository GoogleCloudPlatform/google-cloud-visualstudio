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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers for ToolWindow
    /// </summary>
    internal static class ToolWindowCommandUtils
    {
        /// <summary>
        /// Shows the tool window, either existing or new.
        /// </summary>
        /// <returns>The tool window object.</returns>
        public static Task<TToolWindow> ShowToolWindowAsync<TToolWindow>() where TToolWindow : ToolWindowPane
        {
            return ShowToolWindowAsync<TToolWindow>(0);
        }

        /// <summary>
        /// Shows the tool window for a given id, either existing or new.
        /// </summary>
        /// <returns>The tool window object.</returns>
        public static async Task<TToolWindow> ShowToolWindowAsync<TToolWindow>(int id) where TToolWindow : ToolWindowPane
        {
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = GoogleCloudExtensionPackage.Instance.FindToolWindow<TToolWindow>(true, id);
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            var windowFrame = (IVsWindowFrame)window?.Frame;
            if (windowFrame == null)
            {
                throw new NotSupportedException("Failed to create the tool window");
            }

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            return window;
        }

        /// <summary>
        /// Creates a new instance of a multi-instance tool window.
        /// </summary>
        /// <returns>The tool window object if it is found.</returns>
        public static async Task<TToolWindow> AddToolWindowAsync<TToolWindow>() where TToolWindow : ToolWindowPane
        {
            // Find the first unused tool window id.
            for (var id = 0; true; id++)
            {
                var window = GoogleCloudExtensionPackage.Instance.FindToolWindow<TToolWindow>(false, id);
                if (window == null)
                {
                    // Create a new tool window at the unused id.
                    return await ShowToolWindowAsync<TToolWindow>(id);
                }
            }
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
            menuCommand.Enabled = !string.IsNullOrWhiteSpace(CredentialsStore.Default.CurrentProjectId);
        }
    }
}
