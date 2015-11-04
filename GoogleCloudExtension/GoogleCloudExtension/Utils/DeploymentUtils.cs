// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GCloud;
using GoogleCloudExtension.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public static class DeploymentUtils
    {
        public static async void DeployProjectAsync(
            DnxProject startupProject,
            IList<DnxProject> projects,
            AspNETRuntime selectedRuntime,
            string versionName,
            bool makeDefault,
            AccountAndProjectId accountAndProject)
        {
            try
            {
                StatusbarHelper.SetText("Deployment to AppEngine started...");
                AppEngineOutputWindow.Activate();
                AppEngineOutputWindow.Clear();
                AppEngineOutputWindow.OutputLine("Deployment to AppEngine started...");
                AppEngineOutputWindow.OutputLine($"Deploying project {startupProject} using runtime {DnxRuntime.GetRuntimeDisplayName(selectedRuntime)}.");
                AppEngineOutputWindow.OutputLine($"Deploying to cloud project id {accountAndProject.ProjectId} for acccount {accountAndProject.Account}");
                StatusbarHelper.Freeze();
                GoogleCloudExtensionPackage.IsDeploying = true;

                StatusbarHelper.ShowDeployAnimation();
                await GCloudWrapper.DefaultInstance.DeployApplication(
                    startupProjectPath: startupProject.Root,
                    projectPaths: projects.Select(x => x.Root).ToList(),
                    versionName: versionName,
                    runtime: selectedRuntime,
                    makeDefaultVersion: makeDefault,
                    callback: AppEngineOutputWindow.OutputLine,
                    accountAndProject: accountAndProject);
                StatusbarHelper.UnFreeze();

                StatusbarHelper.SetText("Deployment Succeeded");
                AppEngineOutputWindow.OutputLine("Deployment Succeeded!!!");
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Deployment Failed!!!");
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
        }
    }
}
