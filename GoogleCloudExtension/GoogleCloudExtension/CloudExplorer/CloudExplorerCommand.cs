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

using System;
using System.ComponentModel.Design;
using System.Threading;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// Command handler for the CloudExplorerCommand that opens the Tool Window.
    /// </summary>
    internal static class CloudExplorerCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        private const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid s_commandSet = new Guid("a7435138-27e2-410c-9d28-dffc5aa3fe80");

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="token">The cancellation token.</param>
        public static async Task InitializeAsync(IGoogleCloudExtensionPackage package, CancellationToken token)
        {
            object menuCommandService = await package.GetServiceAsync(typeof(IMenuCommandService));
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            if (menuCommandService is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(s_commandSet, CommandId);
                var menuItem = new MenuCommand(ShowToolWindow, menuCommandID);
                commandService.AddCommand(menuItem);
            }

            // <summary>
            // Shows the tool window when the menu item is clicked.
            // </summary>
            // <param name="sender">The event sender.</param>
            // <param name="e">The event args.</param>
            void ShowToolWindow(object sender, EventArgs e)
            {
                EventsReporterWrapper.EnsureAnalyticsOptIn();

                ErrorHandlerUtils.HandleExceptionsAsync(ShowToolWindowAsync);
            }

            async Task ShowToolWindowAsync()
            {
                // Get the instance number 0 of this tool window. This window is single instance so this instance
                // is actually the only one.
                // The last flag is set to true so that if the tool window does not exists it will be created.
                var window = package.FindToolWindow<CloudExplorerToolWindow>(true);
                if (window?.Frame == null)
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                var windowFrame = (IVsWindowFrame)window.Frame;
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
        }
    }
}
