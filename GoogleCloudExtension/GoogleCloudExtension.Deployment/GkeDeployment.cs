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

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains the logic to deploy ASP.NET Core apps to GKE.
    /// </summary>
    public static class GkeDeployment
    {
        // Wait for up to 5 mins when waiting for a new service's IP address.
        private static readonly TimeSpan s_newServiceIpTimeout = new TimeSpan(0, 5, 0);

        // Wait for up to 2 seconds in between calls when polling.
        private static readonly TimeSpan s_pollingDelay = new TimeSpan(0, 0, 2);

        /// <summary>
        /// The options that define an app's deployment. All options are required.
        /// </summary>
        public class DeploymentOptions
        {
            /// <summary>
            /// The cluster where to deploy the app.
            /// </summary>
            public string Cluster { get; set; }

            /// <summary>
            /// The zone on which the cluster resides.
            /// </summary>
            public string Zone { get; set; }

            /// <summary>
            /// The name to use for the deployment.
            /// </summary>
            public string DeploymentName { get; set; }

            /// <summary>
            /// The version to use for the deployment, will also be used as the version part
            /// in the Docker image tag created.
            /// </summary>
            public string DeploymentVersion { get; set; }

            /// <summary>
            /// Whether to expose a Kubernetes service based on the deployment created. This will be an HTTP service.
            /// </summary>
            public bool ExposeService { get; set; }

            /// <summary>
            /// Whether the service to be exposed should be public or not.
            /// </summary>
            public bool ExposePublicService { get; set; }

            /// <summary>
            /// The context for any gcloud calls to use.
            /// </summary>
            public GCloudContext GCloudContext { get; set; }

            /// <summary>
            /// The context for any kubectl calls to use.
            /// </summary>
            public KubectlContext KubectlContext { get; set; }

            /// <summary>
            /// The number of replicas to create during the deployment.
            /// </summary>
            public int Replicas { get; set; }

            /// <summary>
            /// Callback to be invoked when waiting for a public IP for a service.
            /// </summary>
            public Action WaitingForServiceIpCallback { get; set; }
        }

        /// <summary>
        /// Publishes the ASP.NET Core app using the <paramref name="options"/> to produce the right deployment
        /// and service (if needed).
        /// </summary>
        /// <param name="projectPath">The full path to the project.json file of the startup project.</param>
        /// <param name="options">The options to use for the deployment.</param>
        /// <param name="progress">The progress interface for progress notifications.</param>
        /// <param name="outputAction">The output callback to invoke for output from the process.</param>
        /// <returns>Returns a <seealso cref="GkeDeploymentResult"/> if the deployment succeeded null otherwise.</returns>
        public static async Task<GkeDeploymentResult> PublishProjectAsync(
            string projectPath,
            DeploymentOptions options,
            IProgress<double> progress,
            Action<string> outputAction)
        {
            if (!File.Exists(projectPath))
            {
                Debug.WriteLine($"Cannot find {projectPath}, not a valid project.");
                return null;
            }

            var stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);
            progress.Report(0.1);

            using (var cleanup = new Disposable(() => CommonUtils.Cleanup(stageDirectory)))
            {
                var appRootPath = Path.Combine(stageDirectory, "app");
                var buildFilePath = Path.Combine(stageDirectory, "cloudbuild.yaml");

                if (!await ProgressHelper.UpdateProgress(
                        NetCoreAppUtils.CreateAppBundleAsync(projectPath, appRootPath, outputAction),
                        progress,
                        from: 0.1, to: 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return null;
                }

                NetCoreAppUtils.CopyOrCreateDockerfile(projectPath, appRootPath);
                var image = CloudBuilderUtils.CreateBuildFile(
                    project: options.GCloudContext.ProjectId,
                    imageName: options.DeploymentName,
                    imageVersion: options.DeploymentVersion,
                    buildFilePath: buildFilePath);

                if (!await ProgressHelper.UpdateProgress(
                    GCloudWrapper.BuildContainerAsync(buildFilePath, appRootPath, outputAction, options.GCloudContext),
                    progress,
                    from: 0.4, to: 0.7))
                {
                    Debug.WriteLine("Failed to build container.");
                    return null;
                }
                progress.Report(0.7);

                string publicIpAddress = null;
                string clusterIpAddress = null;
                bool deploymentUpdated = false;
                bool deploymentScaled = false;
                bool serviceExposed = false;
                bool serviceUpdated = false;
                bool serviceDeleted = false;

                // Create or update the deployment.
                var deployments = await KubectlWrapper.GetDeploymentsAsync(options.KubectlContext);
                var deployment = deployments?.FirstOrDefault(x => x.Metadata.Name == options.DeploymentName);
                if (deployment == null)
                {
                    Debug.WriteLine($"Creating new deployment {options.DeploymentName}");
                    if (!await KubectlWrapper.CreateDeploymentAsync(
                            name: options.DeploymentName,
                            imageTag: image,
                            replicas: options.Replicas,
                            outputAction: outputAction,
                            context: options.KubectlContext))
                    {
                        Debug.WriteLine($"Failed to create deployment {options.DeploymentName}");
                        return null;
                    }
                    progress.Report(0.8);
                }
                else
                {
                    Debug.WriteLine($"Updating existing deployment {options.DeploymentName}");
                    if (!await KubectlWrapper.UpdateDeploymentImageAsync(
                            options.DeploymentName,
                            image,
                            outputAction,
                            options.KubectlContext))
                    {
                        Debug.WriteLine($"Failed to update deployemnt {options.DeploymentName}");
                        return null;
                    }
                    deploymentUpdated = true;

                    // If the deployment already exists but the replicas number requested is not the
                    // same as the existing number we will scale up/down the deployment.
                    if (deployment.Spec.Replicas != options.Replicas)
                    {
                        Debug.WriteLine($"Updating the replicas for the deployment.");
                        if (!await KubectlWrapper.ScaleDeploymentAsync(
                                options.DeploymentName,
                                options.Replicas,
                                outputAction,
                                options.KubectlContext))
                        {
                            Debug.WriteLine($"Failed to scale up deployment {options.DeploymentName}");
                            return null;
                        }
                        deploymentScaled = true;
                    }
                }

                // Expose the service if requested and it is not already exposed.
                var services = await KubectlWrapper.GetServicesAsync(options.KubectlContext);
                var service = services?.FirstOrDefault(x => x.Metadata.Name == options.DeploymentName);
                if (options.ExposeService)
                {
                    var requestedType = options.ExposePublicService ?
                        GkeServiceSpec.LoadBalancerType : GkeServiceSpec.ClusterIpType;
                    if (service != null && service?.Spec?.Type != requestedType)
                    {
                        Debug.WriteLine($"The existing service is {service?.Spec?.Type} the requested is {requestedType}");
                        if (!await KubectlWrapper.DeleteServiceAsync(options.DeploymentName, outputAction, options.KubectlContext))
                        {
                            Debug.WriteLine($"Failed to delete serive {options.DeploymentName}");
                        }
                        service = null; // Now the service is gone, needs to be re-created with the new options.

                        serviceUpdated = true;
                    }

                    if (service == null)
                    {
                        // The service needs to be exposed but it wasn't. Expose a new service here.
                        if (!await KubectlWrapper.ExposeServiceAsync(
                            options.DeploymentName,
                            options.ExposePublicService,
                            outputAction,
                            options.KubectlContext))
                        {
                            Debug.WriteLine($"Failed to expose service {options.DeploymentName}");
                            return null;
                        }
                        clusterIpAddress = await WaitForServiceClusterIpAddressAsync(options.DeploymentName, options.KubectlContext);

                        if (options.ExposePublicService)
                        {
                            publicIpAddress = await WaitForServicePublicIpAddressAsync(
                                options.DeploymentName,
                                options.WaitingForServiceIpCallback,
                                options.KubectlContext);
                        }

                        serviceExposed = true;
                    }
                }
                else
                {
                    // The user doesn't want a service exposed.
                    if (service != null)
                    {
                        if (!await KubectlWrapper.DeleteServiceAsync(options.DeploymentName, outputAction, options.KubectlContext))
                        {
                            Debug.WriteLine($"Failed to delete service {options.DeploymentName}");
                            return null;
                        }
                    }

                    serviceDeleted = true;
                }

                return new GkeDeploymentResult(
                    publicIpAddress: publicIpAddress,
                    privateIpAddress: clusterIpAddress,
                    serviceExposed: serviceExposed,
                    serviceUpdated: serviceUpdated,
                    serviceDeleted: serviceDeleted,
                    deploymentUpdated: deploymentUpdated,
                    deploymentScaled: deploymentScaled);
            }
        }

        private static async Task<string> WaitForServiceClusterIpAddressAsync(string name, KubectlContext context)
        {
            var service = await KubectlWrapper.GetServiceAsync(name, context);
            return service?.Spec?.ClusterIp;
        }

        private static async Task<string> WaitForServicePublicIpAddressAsync(string name, Action waitingCallback, KubectlContext kubectlContext)
        {
            DateTime start = DateTime.Now;
            TimeSpan actualTime = DateTime.Now - start;
            while (actualTime < s_newServiceIpTimeout)
            {
                waitingCallback();
                var service = await KubectlWrapper.GetServiceAsync(name, kubectlContext);
                var ingress = service?.Status?.LoadBalancer?.Ingress?.FirstOrDefault();
                if (ingress != null)
                {
                    string ipAddress = null;
                    if (ingress.TryGetValue("ip", out ipAddress))
                    {
                        Debug.WriteLine($"Found service IP address: {ipAddress}");
                        return ipAddress;
                    }
                }
                Debug.WriteLine("Waiting for service to be public.");
                await Task.Delay(s_pollingDelay);
                actualTime = DateTime.Now - start;
            }

            Debug.WriteLine("Timeout while waiting for the ip address.");
            return null;
        }
    }
}
