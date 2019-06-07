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

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains the logic to deploy ASP.NET Core apps to GKE.
    /// </summary>
    [Export(typeof(IGkeDeploymentService))]
    public class GkeDeploymentService : IGkeDeploymentService
    {
        // Wait for up to 5 minutes when waiting for a new service's IP address.
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

            /// <summary>
            /// If true, the deployment service will open the website in a browser after finishing the deployment.
            /// </summary>
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
            public bool Failed { get; }

            /// <summary>
            /// The IP address of the public service if one was exposed. This property can be null
            /// if no public service was exposed or if there was a timeout trying to obtain the public
            /// IP address.
            /// </summary>
            public string ServicePublicIpAddress { get; }

            /// <summary>
            /// The IP address within the cluster for the service. This property can only be null if there
            /// was an error deploying the app.
            /// </summary>
            public string ServiceClusterIpAddress { get; }

            /// <summary>
            /// Is true if the a service was exposed.
            /// </summary>
            public bool ServiceExposed { get; }

            private Result(
                bool failed,
                bool serviceExposed = false,
                string serviceClusterIpAddress = null,
                string servicePublicIpAddress = null)
            {
                Failed = failed;
                ServiceExposed = serviceExposed;
                ServiceClusterIpAddress = serviceClusterIpAddress;
                ServicePublicIpAddress = servicePublicIpAddress;
            }

            public static Result FailedResult { get; } = new Result(true);
            public static Result SuccessResult { get; } = new Result(false);

            public static Result GetPublicServiceResult(string clusterIpAddress, string publicIpAddress) =>
                new Result(false, true, clusterIpAddress, publicIpAddress);

            public static Result GetClusterServiceResult(string clusterIpAddress) => new Result(false, true, clusterIpAddress);
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
                await GcpOutputWindow.ClearAsync();
                await GcpOutputWindow.ActivateAsync();
                await GcpOutputWindow.OutputLineAsync(string.Format(Resources.GkePublishDeployingToGkeMessage, project.Name));

                TimeSpan deploymentDuration;
                Result result;
                using (await StatusbarService.FreezeAsync())
                using (await StatusbarService.ShowDeployAnimationAsync())
                using (IDisposableProgress progress =
                    await StatusbarService.ShowProgressBarAsync(Resources.GkePublishDeploymentStatusMessage))
                using (await ShellUtils.SetShellUIBusyAsync())
                {
                    DateTime deploymentStartTime = DateTime.Now;

                    string imageTag = await BuildImageAsync(project, options, progress);
                    if (imageTag != null)
                    {
                        result = await PublishImageToGkeAsync(imageTag, options, progress);
                    }
                    else
                    {
                        result = Result.FailedResult;
                    }

                    deploymentDuration = DateTime.Now - deploymentStartTime;
                }

                OutputResultData(project, options, result);

                if (options.OpenWebsite && result.ServiceExposed && result.ServicePublicIpAddress != null)
                {
                    BrowserService.OpenBrowser($"http://{result.ServicePublicIpAddress}");
                }

                if (result.Failed)
                {
                    await StatusbarService.SetTextAsync(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Failure));
                }
                else
                {
                    await StatusbarService.SetTextAsync(Resources.PublishSuccessStatusMessage);

                    EventsReporterWrapper.ReportEvent(
                        GkeDeployedEvent.Create(CommandStatus.Success, deploymentDuration));
                }
            }
            catch (Exception)
            {
                await GcpOutputWindow.OutputLineAsync(
                    string.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                await StatusbarService.SetTextAsync(Resources.PublishFailureStatusMessage);
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
        /// <returns>Returns a <seealso cref="Result"/> if the deployment succeeded null otherwise.</returns>
        private async Task<Result> PublishImageToGkeAsync(string imageTag, Options options, IProgress<double> progress)
        {
            bool success = await CreateOrUpdateDeploymentAsync(imageTag, options);
            await progress.ReportAsync(0.8);
            if (!success)
            {
                return Result.FailedResult;
            }

            if (!options.ExposeService)
            {
                return await DeleteExistingServiceAsync(options);
            }
            else
            {
                return await ExposeOrUpdateServiceAsync(options);
            }
        }

        private async Task<Result> ExposeOrUpdateServiceAsync(Options options)
        {
            GkeService existingService = await options.KubectlContext.GetServiceAsync(options.DeploymentName);
            if (existingService == null)
            {
                return await ExposeNewServiceAsync(options.DeploymentName, options);
            }
            else
            {
                return await UpdateExistingServiceAsync(existingService, options);
            }
        }

        private async Task<Result> DeleteExistingServiceAsync(Options options)
        {
            GkeService existingService = await options.KubectlContext.GetServiceAsync(options.DeploymentName);
            if (existingService == null || await DeleteServiceAsync(existingService.Metadata.Name, options))
            {
                return Result.SuccessResult;
            }
            else
            {
                return Result.FailedResult;
            }
        }

        private async Task<bool> DeleteServiceAsync(string service, Options options) => await options.KubectlContext.DeleteServiceAsync(service, GcpOutputWindow.OutputLineAsync);

        private async Task<string> BuildImageAsync(IParsedProject project, Options options, IProgress<double> progress)
        {
            ShellUtils.SaveAllFiles();

            string stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            await progress.ReportAsync(0.1);

            using (new Disposable(() => CommonUtils.Cleanup(stageDirectory)))
            {
                Task<bool> createAppBundleTask = NetCoreAppUtils.CreateAppBundleAsync(
                    project,
                    stageDirectory,
                    GcpOutputWindow.OutputLineAsync,
                    options.Configuration);
                if (!await progress.UpdateProgressAsync(createAppBundleTask, 0.1, 0.3))
                {
                    return null;
                }

                NetCoreAppUtils.CopyOrCreateDockerfile(project, stageDirectory);
                string imageTag = CloudBuilderUtils.GetImageTag(
                    options.KubectlContext.ProjectId,
                    options.DeploymentName,
                    options.DeploymentVersion);

                Task<bool> buildContainerTask =
                    options.KubectlContext.BuildContainerAsync(imageTag, stageDirectory, GcpOutputWindow.OutputLineAsync);
                if (!await progress.UpdateProgressAsync(buildContainerTask, 0.4, 0.7))
                {
                    return null;
                }

                return imageTag;
            }
        }

        private async Task<Result> UpdateExistingServiceAsync(GkeService existingService, Options options)
        {
            string requestedType = options.ExposePublicService ?
                GkeServiceSpec.LoadBalancerType :
                GkeServiceSpec.ClusterIpType;
            string existingServiceName = existingService.Metadata.Name;
            if (existingService.Spec.Type == requestedType)
            {
                return await GetExposedServiceResultAsync(existingServiceName, options);
            }
            else
            {
                if (await DeleteServiceAsync(existingServiceName, options))
                {
                    return await ExposeNewServiceAsync(existingServiceName, options);
                }
                else
                {
                    return Result.FailedResult;
                }
            }
        }

        private async Task<Result> ExposeNewServiceAsync(string serviceName, Options options)
        {
            bool serviceExposed = await options.KubectlContext.ExposeServiceAsync(
                serviceName,
                options.ExposePublicService,
                GcpOutputWindow.OutputLineAsync);

            if (serviceExposed)
            {
                return await GetExposedServiceResultAsync(serviceName, options);
            }
            else
            {
                return Result.FailedResult;
            }
        }

        private async Task<Result> GetExposedServiceResultAsync(string serviceName, Options options)
        {
            string clusterServiceIpAddress =
                await options.KubectlContext.GetServiceClusterIpAsync(serviceName);

            if (options.ExposePublicService)
            {
                string publicIpAddress = await WaitForServicePublicIpAddressAsync(options);
                return Result.GetPublicServiceResult(clusterServiceIpAddress, publicIpAddress);
            }
            else
            {
                return Result.GetClusterServiceResult(clusterServiceIpAddress);
            }
        }

        private async Task<bool> CreateOrUpdateDeploymentAsync(string imageTag, Options options)
        {
            if (options.ExistingDeployment == null)
            {
                bool deploymentCreated =
                    await options.KubectlContext.CreateDeploymentAsync(
                        options.DeploymentName,
                        imageTag,
                        options.Replicas,
                        GcpOutputWindow.OutputLineAsync);
                return deploymentCreated;
            }
            else
            {
                Task<bool> updateImageTask = options.KubectlContext.UpdateDeploymentImageAsync(
                    options.DeploymentName,
                    imageTag,
                    GcpOutputWindow.OutputLineAsync);

                if (options.ExistingDeployment.Spec.Replicas != options.Replicas)
                {
                    bool deploymentScaled =
                        await options.KubectlContext.ScaleDeploymentAsync(
                            options.DeploymentName,
                            options.Replicas,
                            GcpOutputWindow.OutputLineAsync);
                    return deploymentScaled && await updateImageTask;
                }

                return await updateImageTask;
            }
        }

        private async Task<string> WaitForServicePublicIpAddressAsync(Options options)
        {
            DateTime start = DateTime.Now;
            TimeSpan actualTime = DateTime.Now - start;
            await GcpOutputWindow.OutputLineAsync(Resources.GkePublishWaitingForServiceIpMessage);
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
                if (result.ServicePublicIpAddress != null)
                {
                    GcpOutputWindow.OutputLine(
                        string.Format(
                            Resources.GkePublishServiceIpMessage,
                            options.DeploymentName,
                            result.ServicePublicIpAddress));
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
                            result.ServiceClusterIpAddress));
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
