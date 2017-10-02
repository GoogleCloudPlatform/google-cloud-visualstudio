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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace GoogleCloudExtension.GenerateConfigurationCommand
{
    /// <summary>
    /// This class implements the command handler for the generate app.yaml and Dockerfile menu item shown in the project's context
    /// menu in the "Solution Exlorer".
    /// </summary>
    internal sealed class GenerateConfigurationContextMenuCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4227;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a7435138-27e2-410c-9d28-dffc5aa3fe80");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleCloudExtension.PublishDialog.PublishProjectMainMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private GenerateConfigurationContextMenuCommand(Package package)
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
                var menuItem = new OleMenuCommand(OnGenerateConfiguration, menuCommandID);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateConfigurationContextMenuCommand Instance
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
            Instance = new GenerateConfigurationContextMenuCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnGenerateConfiguration(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                var selectedProject = SolutionHelper.CurrentSolution.SelectedProject;
                Debug.WriteLine($"Generating configuration for project: {selectedProject.FullPath}");
                var configurationStatus = AppEngineFlexDeployment.CheckProjectConfiguration(selectedProject);

                // If the app.yaml already exists allow the user to skip its generation to preserve the existing file.
                if (!configurationStatus.HasAppYaml ||
                    UserPromptUtils.ActionPrompt(
                        prompt: Resources.GenerateConfigurationAppYamlOverwriteMessage,
                        title: Resources.GenerateConfigurationOverwritePromptTitle,
                        actionCaption: Resources.UiOverwriteButtonCaption,
                        cancelCaption: Resources.UiSkipFileButtonCaption))
                {
                    Debug.WriteLine($"Generating app.yaml for {selectedProject.FullPath}");
                    if (!AppEngineFlexDeployment.GenerateAppYaml(selectedProject))
                    {
                        UserPromptUtils.ErrorPrompt(
                            String.Format(Resources.GenerateConfigurationFileGenerationErrorMessage, AppEngineFlexDeployment.AppYamlName),
                            Resources.GenerateConfigurationFileGeneratinErrorTitle);
                        return;
                    }
                    GcpOutputWindow.OutputLine(Resources.GenerateConfigurationAppYamlGeneratedMessage);
                }

                // If the Dockerfile already exists allow the user to skip its generation to preserve the existing file.
                if (!configurationStatus.HasDockerfile ||
                    UserPromptUtils.ActionPrompt(
                        prompt: Resources.GenerateConfigurationDockerfileOverwriteMessage,
                        title: Resources.GenerateConfigurationOverwritePromptTitle,
                        actionCaption: Resources.UiOverwriteButtonCaption,
                        cancelCaption: Resources.UiSkipFileButtonCaption))
                {
                    Debug.WriteLine($"Generating Dockerfile for {selectedProject.FullPath}");
                    if (!AppEngineFlexDeployment.GenerateDockerfile(selectedProject))
                    {
                        UserPromptUtils.ErrorPrompt(
                            String.Format(Resources.GenerateConfigurationFileGenerationErrorMessage, AppEngineFlexDeployment.DockerfileName),
                            Resources.GenerateConfigurationFileGeneratinErrorTitle);
                        return;
                    }
                    GcpOutputWindow.OutputLine(Resources.GenerateConfigurationDockerfileGeneratedMessage);
                }
            });
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            // Ensure that the menu entry is only available for ASP.NET Core projects.
            var selectedProject = SolutionHelper.CurrentSolution.SelectedProject;
            if (selectedProject == null || !selectedProject.IsAspNetCoreProject())
            {
                menuCommand.Visible = false;
            }
            else
            {
                menuCommand.Visible = true;
                menuCommand.Enabled = !ShellUtils.IsBusy();
            }
        }
    }
}
