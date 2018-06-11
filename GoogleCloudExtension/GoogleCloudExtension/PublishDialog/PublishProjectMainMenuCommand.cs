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

using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// This class implements the command handler for the menu item shown under "Tools > Google Cloud Tools".
    /// </summary>
    internal sealed class PublishProjectMainMenuCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4225;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a7435138-27e2-410c-9d28-dffc5aa3fe80");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishProjectMainMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PublishProjectMainMenuCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(OnDeployCommand, menuCommandID);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PublishProjectMainMenuCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return _package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new PublishProjectMainMenuCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnDeployCommand(object sender, EventArgs e)
        {
            var project = SolutionHelper.CurrentSolution.StartupProject.ParsedProject;
            Debug.WriteLine($"Deploying project: {project.FullPath}");
            PublishDialogWindow.PromptUser(project);
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            menuCommand.Visible = true;

            var startupProject = SolutionHelper.CurrentSolution.StartupProject?.ParsedProject;
            if (startupProject == null)
            {
                menuCommand.Enabled = false;
                menuCommand.Text = Resources.PublishDialogGenericMenuHeader;
            }
            else
            {
                menuCommand.Enabled = PublishDialogWindow.CanPublish(startupProject) && !ShellUtils.Default.IsBusy();
                menuCommand.Text = String.Format(Resources.PublishDialogProjectMenuHeader, startupProject.Name);
            }
        }
    }
}
