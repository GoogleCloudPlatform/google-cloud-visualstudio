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

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains the logic to deploy ASP.NET Core apps to GKE.
    /// </summary>
    [Export(typeof(IGkeDeploymentService))]
    public class GkeDeploymentService : IGkeDeploymentService
    {
        // Wait for up to 5 mins when waiting for a new service's IP address.
        private static readonly TimeSpan s_newServiceIpTimeout = new TimeSpan(0, 5, 0);

        // Wait for up to 2 seconds in between calls when polling.
        private static readonly TimeSpan s_pollingDelay = new TimeSpan(0, 0, 2);

        private readonly Lazy<IGcpOutputWindow> _gcpOutputWindow;
        private readonly Lazy<IStatusbarService> _statusbarService;
        private readonly Lazy<IShellUtils> _shellUtils;

        private IStatusbarService StatusbarService => _statusbarService.Value;
        private IShellUtils ShellUtils => _shellUtils.Value;
        private readonly Lazy<IBrowserService> _browserService;
        private readonly Lazy<INetCoreAppUtils> _netCoreAppUtils;
        private IGcpOutputWindow GcpOutputWindow => _gcpOutputWindow.Value;
        private IBrowserService BrowserService => _browserService.Value;
        private INetCoreAppUtils NetCoreAppUtils => _netCoreAppUtils.Value;

        [ImportingConstructor]
        public GkeDeploymentService(
            Lazy<IGcpOutputWindow> gcpOutputWindow,
            Lazy<IStatusbarService> statusbarService,
            Lazy<IShellUtils> shellUtils,
            Lazy<IBrowserService> browserService,
            Lazy<INetCoreAppUtils> netCoreAppUtils)
        {
            _shellUtils = shellUtils;
            _gcpOutputWindow = gcpOutputWindow;
            _statusbarService = statusbarService;
            _browserService = browserService;
            _netCoreAppUtils = netCoreAppUtils;
        }


        /// <summary>
        /// The options that define an app's deployment. All options are required.
        /// </summary>
        public class Options
        {
            public Options(
                IKubectlContext context,
                string deploymentName,
                string deploymentVersion,
                GkeDeployment existingDeployment,
                bool exposeService,
                bool exposePublicService,
                bool openWebsite,
                string configuration,
                int replicas)
            {
                KubectlContext = context;
                DeploymentName = deploymentName;
                DeploymentVersion = deploymentVersion;
                ExistingDeployment = existingDeployment;
                ExposeService = exposeService;
                ExposePublicService = exposePublicService;
                OpenWebsite = openWebsite;
                Configuration = configuration;
                Replicas = replicas;
            }

            /// <summary>
            /// The name to use for the deployment.
            /// </summary>
            public string DeploymentName { get; }

            /// <summary>
            /// The version to use for the deployment, will also be used as the version part
            /// in the Docker image tag created.
            /// </summary>
            public string DeploymentVersion { get; }

            public GkeDeployment ExistingDeployment { get; }

            /// <summary>
            /// Whether to expose a Kubernetes service based on the deployment created. This will be an HTTP service.
            /// </summary>
            public bool ExposeService { get; }

            /// <summary>
            /// Whether the service to be exposed should be public or not.
            /// </summary>
            public bool ExposePublicService { get; }

            public bool OpenWebsite { get; }

            /// <summary>
            /// The context for any kubectl calls to use.
            /// </summary>
            public IKubectlContext KubectlContext { get; }

            /// <summary>
            /// The name of the configuration to publish.
            /// </summary>
            public string Configuration { get; }

            /// <summary>
            /// The number of replicas to create during the deployment.
            /// </summary>
            public int Replicas { get; }
        }

        /// <summary>
        /// This class contains the result of a GKE deployment.
        /// </summary>
        private class Result
        {
            /// <summary>
            /// True when some step of the deployment to GKE failed.
            /// </summary>
            public bool Failed { get; set; }

            /// <summary>
            /// The IP address of the public service if one was exposed. This property can be null
            /// if no public service was exposed or if there was a timeout trying to obtain the public
            /// IP address.
            /// </summary>
            public string PublicServiceIpAddress { get; set; }

            /// <summary>
            /// The IP address within the cluster for the service. This property can only be null if there
            /// was an error deploying the app.
            /// </summary>
            public string ClusterServiceIpAddress { get; set; }

            /// <summary>
            /// Is true if the a service was exposed publicly.
            /// </summary>
            public bool ServiceExposed { get; set; }
        }

        /// <summary>
        /// Builds the project and deploys it to Google Kubernetes Engine.
        /// </summary>
        /// <param name="project">The project to build and deploy.</param>
        /// <param name="options">Options for deploying and building.</param>
        public async Task DeployProjectToGkeAsync(IParsedProject project, Options options)
        {
            try
            {
                GcpOutputWindow.Clear();
                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishDeployingToGkeMessage, project.Name));

                TimeSpan deploymentDuration;
                Result result;
                using (StatusbarService.Freeze())
                using (StatusbarService.ShowDeployAnimation())
                using (IDisposableProgress progress =
                    StatusbarService.ShowProgressBar(Resources.GkePublishDeploymentStatusMessage))
                using (ShellUtils.SetShellUIBusy())
                {
                    DateTime deploymentStartTime = DateTime.Now;
                    result = new Result();

                    string imageTag = await BuildImageAsync(project, options, progress, result);
                    if (result.Failed)
                    {
                        return;
                    }

                    await PublishImageToGkeAsync(imageTag, options, progress, result);
                    deploymentDuration = DateTime.Now - deploymentStartTime;
                }

                OutputResultData(project, options, result);

                if (options.OpenWebsite && result.ServiceExposed && result.PublicServiceIpAddress != null)
                {
                    BrowserService.OpenBrowser($"http://{result.PublicServiceIpAddress}");
                }

                if (result.Failed)
                {
                    StatusbarService.SetText(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Failure));
                }
                else
                {
                    StatusbarService.SetText(Resources.PublishSuccessStatusMessage);

                    EventsReporterWrapper.ReportEvent(
                        GkeDeployedEvent.Create(CommandStatus.Success, deploymentDuration));
                }
            }
            catch (Exception)
            {
                GcpOutputWindow.OutputLine(
                    string.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                StatusbarService.SetText(Resources.PublishFailureStatusMessage);
                EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        /// <summary>
        /// Publishes the ASP.NET Core app using the <paramref name="options"/> to produce the right deployment
        /// and service (if needed).
        /// </summary>
        /// <param name="imageTag"></param>
        /// <param name="options">The options to use for the deployment.</param>
        /// <param name="progress">The progress interface for progress notifications.</param>
        /// <param name="result"></param>
        /// <returns>Returns a <seealso cref="Result"/> if the deployment succeeded null otherwise.</returns>
        private async Task PublishImageToGkeAsync(
            string imageTag,
            Options options,
            IProgress<double> progress,
            Result result)
        {

            // Create or update the deployment.
            await CreateOrUpdateDeploymentAsync(imageTag, options, progress, result);

            if (result.Failed)
            {
                return;
            }

            // Expose the service if requested and it is not already exposed.
            if (options.ExposeService)
            {
                await UpdateOrExposeServiceAsync(options, result);
            }
            else
            {
                IList<GkeService> services = await options.KubectlContext.GetServicesAsync();
                GkeService service = services?.FirstOrDefault(x => x.Metadata.Name == options.DeploymentName);
                // The user doesn't want a service exposed.
                if (service != null)
                {
                    bool serviceDeleted = await options.KubectlContext.DeleteServiceAsync(
                        options.DeploymentName,
                        GcpOutputWindow.OutputLine);
                    result.Failed |= !serviceDeleted;
                }
            }
        }


        private async Task<string> BuildImageAsync(
            IParsedProject project,
            Options options,
            IProgress<double> progress,
            Result result)
        {
            ShellUtils.SaveAllFiles();

            string stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            progress.Report(0.1);

            using (new Disposable(() => CommonUtils.Cleanup(stageDirectory)))
            {
                Task<bool> createAppBundleTask = NetCoreAppUtils.CreateAppBundleAsync(
                    project,
                    stageDirectory,
                    GcpOutputWindow.OutputLine,
                    options.Configuration);
                if (!await progress.UpdateProgress(createAppBundleTask, 0.1, 0.3))
                {
                    result.Failed = true;
                    return null;
                }

                NetCoreAppUtils.CopyOrCreateDockerfile(project, stageDirectory);
                string imageTag = CloudBuilderUtils.GetImageTag(
                    options.KubectlContext.ProjectId,
                    options.DeploymentName,
                    options.DeploymentVersion);

                Task<bool> buildContainerTask =
                    options.KubectlContext.BuildContainerAsync(imageTag, stageDirectory, GcpOutputWindow.OutputLine);
                if (!await progress.UpdateProgress(buildContainerTask, 0.4, 0.7))
                {
                    result.Failed = true;
                }

                return imageTag;
            }
        }

        private async Task UpdateOrExposeServiceAsync(
            Options options,
            Result result)
        {
            GkeService service = await options.KubectlContext.GetServiceAsync(options.DeploymentName);
            string requestedType = options.ExposePublicService ?
                GkeServiceSpec.LoadBalancerType :
                GkeServiceSpec.ClusterIpType;

            if (service == null)
            {
                await ExposeServiceAsync(options, result);
            }
            else if (service.Spec?.Type != requestedType)
            {
                if (!await options.KubectlContext.DeleteServiceAsync(
                    options.DeploymentName,
                    GcpOutputWindow.OutputLine))
                {
                    result.Failed = true;
                    return;
                }

                await ExposeServiceAsync(options, result);
            }
            else
            {
                result.ServiceExposed = true;
                result.PublicServiceIpAddress = await options.KubectlContext.GetPublicServiceIpAsync(options.DeploymentName);
            }
        }

        private async Task ExposeServiceAsync(Options options, Result result)
        {
            // The service needs to be exposed but it wasn't. Expose a new service here.
            bool serviceExposed = await options.KubectlContext.ExposeServiceAsync(
                options.DeploymentName,
                options.ExposePublicService,
                GcpOutputWindow.OutputLine);
            result.ServiceExposed = serviceExposed;
            if (!serviceExposed)
            {
                result.Failed = true;
                return;
            }

            result.ClusterServiceIpAddress =
                await options.KubectlContext.GetServiceClusterIpAsync(options.DeploymentName);

            if (options.ExposePublicService)
            {
                result.PublicServiceIpAddress = await WaitForServicePublicIpAddressAsync(options);
            }
        }

        private async Task CreateOrUpdateDeploymentAsync(
            string imageTag,
            Options options,
            IProgress<double> progress,
            Result result)
        {
            if (options.ExistingDeployment == null)
            {
                bool deploymentCreated =
                    await options.KubectlContext.CreateDeploymentAsync(
                        options.DeploymentName,
                        imageTag,
                        options.Replicas,
                        GcpOutputWindow.OutputLine);
                result.Failed |= !deploymentCreated;
            }
            else
            {
                Task<bool> updateImageTask = options.KubectlContext.UpdateDeploymentImageAsync(
                    options.DeploymentName,
                    imageTag,
                    GcpOutputWindow.OutputLine);

                if (options.ExistingDeployment.Spec.Replicas != options.Replicas)
                {
                    bool deploymentScaled =
                        await options.KubectlContext.ScaleDeploymentAsync(
                            options.DeploymentName,
                            options.Replicas,
                            GcpOutputWindow.OutputLine);
                    result.Failed |= !deploymentScaled;
                }

                bool imageUpdated = await updateImageTask;
                result.Failed |= !imageUpdated;
            }

            progress.Report(0.8);
        }

        private async Task<string> WaitForServicePublicIpAddressAsync(Options options)
        {
            DateTime start = DateTime.Now;
            TimeSpan actualTime = DateTime.Now - start;
            GcpOutputWindow.OutputLine(Resources.GkePublishWaitingForServiceIpMessage);
            while (actualTime < s_newServiceIpTimeout)
            {
                string ip = await options.KubectlContext.GetPublicServiceIpAsync(options.DeploymentName);
                if (ip != null)
                {
                    return ip;
                }

                actualTime = DateTime.Now - start;
                await Task.Delay(s_pollingDelay);
            }

            return null;
        }

        private void OutputResultData(
            IParsedProject project,
            Options options,
            Result result)
        {
            if (result.ServiceExposed)
            {
                if (result.PublicServiceIpAddress != null)
                {
                    GcpOutputWindow.OutputLine(
                        string.Format(
                            Resources.GkePublishServiceIpMessage,
                            options.DeploymentName,
                            result.PublicServiceIpAddress));
                }
                else if (options.ExposePublicService)
                {
                    GcpOutputWindow.OutputLine(Resources.GkePublishServiceIpTimeoutMessage);
                }
                else
                {
                    GcpOutputWindow.OutputLine(
                        string.Format(
                            Resources.GkePublishServiceClusterIpMessage,
                            options.DeploymentName,
                            result.ClusterServiceIpAddress));
                }
            }

            if (result.Failed)
            {
                GcpOutputWindow.OutputLine(
                    string.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
            }
            else
            {
                GcpOutputWindow.OutputLine(
                    string.Format(Resources.GkePublishDeploymentSuccessMessage, project.Name));
            }
        }
    }
}
