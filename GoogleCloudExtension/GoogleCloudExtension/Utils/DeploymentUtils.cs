﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.DeploymentDialog;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.Projects;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public static class DeploymentUtils
    {
        /// <summary>
        /// Starts the deployment process for the given project, if the project doesn't target the
        /// right runtime or the environment is not set then this becomes a NOOP.
        /// </summary>
        /// <param name="startupProject"></param>
        /// <param name="serviceProvider"></param>
        public static void StartProjectDeployment(Project startupProject, IServiceProvider serviceProvider)
        {
            Debug.WriteLine($"Starting the deployment process for project {startupProject.Name}.");

            // Validate the environment before attempting to start the deployment process.
            if (!CommandUtils.ValidateEnvironment())
            {
                Debug.WriteLine("Invoked when the environment is not valid.");
                VsShellUtilities.ShowMessageBox(
                    serviceProvider,
                    "Please ensure that the Google Cloud SDK is installed and available in the path and that the preview, app and alpha components are installed.",
                    "The Google Cloud SDK command-line tool (gcloud) could not be found",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // We only support the CoreCLR runtime.
            if (startupProject.Runtime == DnxRuntime.Dnx451)
            {
                var window = new DeploymentDialogWindow(new DeploymentDialogWindowOptions
                {
                    Project = startupProject,
                    ProjectsToRestore = SolutionHelper.CurrentSolution.Projects,
                });
                window.ShowModal();
            }
            else
            {
                var runtime = DnxRuntimeInfo.GetRuntimeInfo(startupProject.Runtime);
                ExtensionAnalytics.ReportEvent("RuntimeNotSupportedError", startupProject.Runtime.ToString());
                AppEngineOutputWindow.OutputLine($"Runtime {runtime.DisplayName} is not supported for project {startupProject.Name}");
                VsShellUtilities.ShowMessageBox(
                    serviceProvider,
                    $"Runtime {runtime.DisplayName} is not supported. Project {startupProject.Name} needs to target {DnxRuntimeInfo.Dnx451DisplayString}.",
                    "Runtime not supported",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static async Task<bool> DeployProjectAsync(
            Project startupProject,
            IList<Project> projects,
            string versionName,
            bool makeDefault,
            bool preserveOutput,
            Credentials accountAndProject)
        {
            bool result = false;

            try
            {
                StatusbarHelper.SetText("Deployment to AppEngine started...");
                AppEngineOutputWindow.Activate();
                AppEngineOutputWindow.Clear();
                AppEngineOutputWindow.OutputLine("Deployment to AppEngine started...");
                AppEngineOutputWindow.OutputLine($"Deploying project {startupProject}.");
                AppEngineOutputWindow.OutputLine($"Deploying to cloud project id {accountAndProject.ProjectId} for acccount {accountAndProject.Account}");
                StatusbarHelper.Freeze();
                GoogleCloudExtensionPackage.IsDeploying = true;

                StatusbarHelper.ShowDeployAnimation();
                await AppEngineClient.DeployApplicationAsync(
                    startupProjectPath: startupProject.Root,
                    projectPaths: projects.Select(x => x.Root).ToList(),
                    versionName: versionName,
                    promoteVersion: makeDefault,
                    callback: AppEngineOutputWindow.OutputLine,
                    preserveOutput: preserveOutput,
                    accountAndProject: accountAndProject);
                StatusbarHelper.UnFreeze();

                StatusbarHelper.SetText("Deployment Succeeded");
                AppEngineOutputWindow.OutputLine("Deployment Succeeded.");

                result = true;
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Deployment Failed.");
                AppEngineOutputWindow.OutputLine(ex.Message);
            }
            catch (Exception)
            {
                AppEngineOutputWindow.OutputLine("Deployment Failed!!!");
                StatusbarHelper.SetText("Deployment Failed");
            }
            finally
            {
                StatusbarHelper.UnFreeze();
                StatusbarHelper.HideDeployAnimation();
                GoogleCloudExtensionPackage.IsDeploying = false;
            }

            return result;
        }
    }
}
