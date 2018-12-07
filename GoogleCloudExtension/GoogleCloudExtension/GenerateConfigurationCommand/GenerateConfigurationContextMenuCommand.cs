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
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services.Configuration;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.GenerateConfigurationCommand
{
    /// <summary>
    /// This class implements the command handler for the generate app.yaml and Dockerfile menu item shown in the project's context
    /// menu in the "Solution Exlorer".
    /// </summary>
    internal static class GenerateConfigurationContextMenuCommand
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
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="token"></param>
        public static async Task InitializeAsync(IGoogleCloudExtensionPackage package, CancellationToken token)
        {
            package.ThrowIfNull(nameof(package));

            if (await package.GetServiceAsync(typeof(IMenuCommandService)) is IMenuCommandService commandService)
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(OnGenerateConfiguration, menuCommandID);
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
        private static void OnGenerateConfiguration(object sender, EventArgs e) =>
            ErrorHandlerUtils.HandleExceptionsAsync(GenerateConfigurationAsync);

        /// <summary>
        /// Queries the user and generates an app.yaml and Docker file.
        /// </summary>
        private static async Task GenerateConfigurationAsync()
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            IParsedDteProject selectedProject = SolutionHelper.CurrentSolution.SelectedProject.ParsedProject;
            Debug.WriteLine($"Generating configuration for project: {selectedProject.FullPath}");
            IAppEngineConfiguration appEngineConfiguration = GoogleCloudExtensionPackage.Instance.GetMefService<IAppEngineConfiguration>();
            ProjectConfigurationStatus configurationStatus = appEngineConfiguration.CheckProjectConfiguration(selectedProject);

            // If the app.yaml already exists allow the user to skip its generation to preserve the existing file.
            if (!configurationStatus.HasAppYaml ||
                UserPromptService.Default.ActionPrompt(
                    prompt: Resources.GenerateConfigurationAppYamlOverwriteMessage,
                    title: Resources.GenerateConfigurationOverwritePromptTitle,
                    actionCaption: Resources.UiOverwriteButtonCaption,
                    cancelCaption: Resources.UiSkipFileButtonCaption))
            {
                Debug.WriteLine($"Generating app.yaml for {selectedProject.FullPath}");
                try
                {
                    appEngineConfiguration.AddAppYamlItem(selectedProject);
                }
                catch (Exception error)

                {
                    UserPromptService.Default.ErrorPrompt(
                        string.Format(
                            Resources.GenerateConfigurationFileGenerationErrorMessage,
                            AppEngineConfiguration.AppYamlName),
                        Resources.GenerateConfigurationFileGeneratinErrorTitle,
                        error.Message);
                    return;
                }

                await GcpOutputWindow.Default.OutputLineAsync(Resources.GenerateConfigurationAppYamlGeneratedMessage);
            }

            // If the Dockerfile already exists allow the user to skip its generation to preserve the existing file.
            if (!configurationStatus.HasDockerfile ||
                UserPromptService.Default.ActionPrompt(
                    prompt: Resources.GenerateConfigurationDockerfileOverwriteMessage,
                    title: Resources.GenerateConfigurationOverwritePromptTitle,
                    actionCaption: Resources.UiOverwriteButtonCaption,
                    cancelCaption: Resources.UiSkipFileButtonCaption))
            {
                Debug.WriteLine($"Generating Dockerfile for {selectedProject.FullPath}");
                try
                {
                    GoogleCloudExtensionPackage.Instance.GetMefService<INetCoreAppUtils>()
                        .GenerateDockerfile(selectedProject);
                }
                catch (Exception exception)
                {
                    UserPromptService.Default.ErrorPrompt(
                        string.Format(
                            Resources.GenerateConfigurationFileGenerationErrorMessage,
                            NetCoreAppUtils.DockerfileName),
                        Resources.GenerateConfigurationFileGeneratinErrorTitle,
                        exception.Message);
                    return;
                }

                await GcpOutputWindow.Default.OutputLineAsync(Resources.GenerateConfigurationDockerfileGeneratedMessage);
            }
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is OleMenuCommand menuCommand))
            {
                return;
            }

            // Ensure that the menu entry is only available for ASP.NET Core projects.
            IParsedProject selectedProject = SolutionHelper.CurrentSolution.SelectedProject?.ParsedProject;
            if (selectedProject?.ProjectType != KnownProjectTypes.NetCoreWebApplication)
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
