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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class offers services to perform deployments for ASP.NET 4.x applications to a GCE VM.
    /// </summary>
    [Export(typeof(IWindowsVmDeployment))]
    public class WindowsVmDeployment : IWindowsVmDeployment
    {
        private readonly Lazy<IProcessService> _processService;
        private readonly Lazy<IShellUtils> _shellUtils;
        private readonly Lazy<IStatusbarService> _statusbarService;
        private readonly Lazy<IGcpOutputWindow> _gcpOutputWindow;

        private IProcessService ProcessService => _processService.Value;

        private IStatusbarService StatusbarHelper => _statusbarService.Value;

        private IShellUtils ShellUtils => _shellUtils.Value;
        private IGcpOutputWindow GcpOutputWindow => _gcpOutputWindow.Value;

        [ImportingConstructor]
        public WindowsVmDeployment(
            Lazy<IProcessService> processService,
            Lazy<IShellUtils> shellUtils,
            Lazy<IStatusbarService> statusbarService,
            Lazy<IGcpOutputWindow> gcpOutputWindow)
        {
            _processService = processService;
            _shellUtils = shellUtils;
            _statusbarService = statusbarService;
            _gcpOutputWindow = gcpOutputWindow;
        }

        /// <summary>
        /// Publishes an ASP.NET 4.x project to the given GCE <seealso cref="Instance"/>.
        /// </summary>
        /// <param name="project">The project to deploy.</param>
        /// <param name="targetInstance">The instance to deploy.</param>
        /// <param name="credentials">The Windows credentials to use to deploy to the <paramref name="targetInstance"/>.</param>
        /// <param name="targetDeployPath">The Name or Path of the Website or App to publish</param>
        /// <param name="configuration">The name of the configuration to publish.</param>
        public async Task<bool> PublishProjectAsync(
            IParsedDteProject project,
            Instance targetInstance,
            WindowsInstanceCredentials credentials,
            string targetDeployPath,
            string configuration)
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            ShellUtils.SaveAllFiles();
            // Ensure NuGet packages are restored.
            project.Project.DTE.Solution.SolutionBuild.BuildProject(configuration, project.Project.UniqueName, true);

            await GcpOutputWindow.ClearAsync();
            await GcpOutputWindow.OutputLineAsync(string.Format(Resources.GcePublishStepStartMessage, project.Name));
            await GcpOutputWindow.ActivateAsync();

            string msbuildPath = VsVersionUtils.ToolsPathProvider.GetMsbuildPath();
            MSBuildTarget target;
            switch (project.ProjectType)
            {
                case KnownProjectTypes.WebApplication:
                    target = new MSBuildTarget("WebPublish");
                    break;
                default:
                    target = new MSBuildTarget("Publish");
                    break;
            }
            var parameters = new object[]
            {
                '"' + project.FullPath + '"',
                target,
                new MSBuildProperty("Configuration", configuration),
                new MSBuildProperty("WebPublishMethod", "MSDeploy"),
                new MSBuildProperty("MSDeployPublishMethod", "WMSVC"),
                new MSBuildProperty("MSDeployServiceURL",targetInstance.GetPublishUrl()),
                new MSBuildProperty("DeployIisAppPath", targetDeployPath),
                new MSBuildProperty("UserName", credentials.User),
                new MSBuildProperty("Password", credentials.Password),
                new MSBuildProperty("AllowUntrustedCertificate", "True")
            };
            string publishMessage = string.Format(Resources.GcePublishProgressMessage, targetInstance.Name);
            using (await StatusbarHelper.FreezeTextAsync(publishMessage))
            using (await StatusbarHelper.ShowDeployAnimationAsync())
            using (await ShellUtils.SetShellUIBusyAsync())
            {
                string msBuildParameters = string.Join(" ", parameters);
                bool result = await ProcessService.RunCommandAsync(
                    msbuildPath,
                    msBuildParameters,
                    GcpOutputWindow.OutputLineAsync);

                if (result)
                {
                    return true;
                }

                await TaskScheduler.Default;

                // An initial failure is common, retry.
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                return await ProcessService.RunCommandAsync(
                    msbuildPath,
                    msBuildParameters,
                    GcpOutputWindow.OutputLineAsync);
            }
        }
    }
}
