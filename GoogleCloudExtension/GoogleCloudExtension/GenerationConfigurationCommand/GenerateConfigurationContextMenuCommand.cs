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
    /// This class implements the command handler for the menu item shown in the project's context
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
        /// <Guid("")>
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
            var selectedProject = SolutionHelper.CurrentSolution.SelectedProject;
            Debug.WriteLine($"Generating configuration for project: {selectedProject.FullPath}");
            var configurationStatus = AppEngineFlexDeployment.CheckProjectConfiguration(selectedProject.FullPath);

            if (configurationStatus.HasAppYaml && configurationStatus.HasDockerfile)
            {
                if (!UserPromptUtils.ActionPrompt(
                    prompt: "The files app.yaml and Dockerfile already exist in your project, are you sure you want to overwrite them?",
                    title: "Configuration files already exist",
                    actionCaption: "Overwrite"))
                {
                    return;
                }
            }
            else if (configurationStatus.HasAppYaml)
            {
                if (!UserPromptUtils.ActionPrompt(
                    prompt: "The file app.yaml already exists in your project, are you sure you want to overwrite it?",
                    title: "Configuration files already exist",
                    actionCaption: "Overwrite"))
                {
                    return;
                }
            }
            else if (configurationStatus.HasDockerfile)
            {
                if (!UserPromptUtils.ActionPrompt(
                    prompt: "The file Dockerfile already exists in your project, are you sure you want to overwrite it?",
                    title: "Configuration files already exist",
                    actionCaption: "Overwrite"))
                {
                    return;
                }
            }

            AppEngineFlexDeployment.GenerateConfigurationFiles(selectedProject.FullPath);
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            var selectedProject = SolutionHelper.CurrentSolution.SelectedProject;
            if (selectedProject == null || !PublishDialogWindow.CanPublish(selectedProject))
            {
                menuCommand.Visible = false;
            }
            else
            {
                menuCommand.Visible = true;
                menuCommand.Enabled = !ShellUtils.IsBusy();
                menuCommand.Text = String.Format(Resources.GenerateConfigurationMenuHeader, selectedProject.Name);
            }
        }
    }
}
