// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.Utils
{
    public static class DeploymentUtils
    {
        public static async void DeployProjectAsync(
            Project startupProject,
            IList<Project> projects,
            DnxRuntime selectedRuntime,
            string versionName,
            bool makeDefault,
            Credentials accountAndProject)
        {
            try
            {
                StatusbarHelper.SetText("Deployment to AppEngine started...");
                AppEngineOutputWindow.Activate();
                AppEngineOutputWindow.Clear();
                AppEngineOutputWindow.OutputLine("Deployment to AppEngine started...");
                var displayName = DnxRuntimeInfo.GetRuntimeInfo(selectedRuntime).DisplayName;
                AppEngineOutputWindow.OutputLine($"Deploying project {startupProject} using runtime {displayName}.");
                AppEngineOutputWindow.OutputLine($"Deploying to cloud project id {accountAndProject.ProjectId} for acccount {accountAndProject.Account}");
                StatusbarHelper.Freeze();
                GoogleCloudExtensionPackage.IsDeploying = true;

                StatusbarHelper.ShowDeployAnimation();
                await AppEngineClient.DeployApplicationAsync(
                    startupProjectPath: startupProject.Root,
                    projectPaths: projects.Select(x => x.Root).ToList(),
                    versionName: versionName,
                    runtime: selectedRuntime,
                    promoteVersion: makeDefault,
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
