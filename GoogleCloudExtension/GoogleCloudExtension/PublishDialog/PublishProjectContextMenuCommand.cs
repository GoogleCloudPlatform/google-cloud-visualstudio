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
using System.Diagnostics;
using System.Threading;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// This class implements the command handler for the menu item shown in the project's context
    /// menu in the "Solution Exlorer".
    /// </summary>
    internal static class PublishProjectContextMenuCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4226;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a7435138-27e2-410c-9d28-dffc5aa3fe80");

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="token"></param>
        public static async Task InitializeAsync(IGoogleCloudExtensionPackage package, CancellationToken token)
        {
            package.ThrowIfNull(nameof(package));

            if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(OnDeployCommand, menuCommandID);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void OnDeployCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                IParsedDteProject selectedProject = SolutionHelper.CurrentSolution.SelectedProject.ParsedProject;
                Debug.WriteLine($"Deploying project: {selectedProject.FullPath}");
                PublishDialogWindow.PromptUser(selectedProject);
            });
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is OleMenuCommand menuCommand))
            {
                return;
            }

            IParsedDteProject selectedProject = SolutionHelper.CurrentSolution.SelectedProject?.ParsedProject;
            if (selectedProject == null || !PublishDialogWindow.CanPublish(selectedProject))
            {
                menuCommand.Visible = false;
            }
            else
            {
                menuCommand.Visible = true;
                menuCommand.Enabled = !ShellUtils.Default.IsBusy();
            }
        }
    }
}
