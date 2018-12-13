// Copyright 2018 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// The types of clusters. Currently there are clusters in a region and clusters in a zone.
    /// </summary>
    public enum ClusterLocationType { Zone, Region }

    /// <summary>
    /// This class owns the context on which to run kubectl commands. This class owns
    /// the config file, when the instance is disposed it will delete the file.
    /// </summary>
    public class KubectlContext : GCloudContext, IDisposable, IKubectlContext
    {
        // This variable is used to override the location of the application default credentials
        // with the current user's credentials.
        internal const string GoogleApplicationCredentialsVariable = "GOOGLE_APPLICATION_CREDENTIALS";

        // This variable contains the path to the configuration to be used for kubernetes operations.
        internal const string KubeConfigVariable = "KUBECONFIG";

        // This variables is used to force gcloud to use application default credentials when generating
        // the cluster information for the cluster.
        internal const string UseApplicationDefaultCredentialsVariable =
            "CLOUDSDK_CONTAINER_USE_APPLICATION_DEFAULT_CREDENTIALS";

        internal const string TrueValue = "true";

        /// <summary>
        /// Path to the config file that identifies the cluster for kubectl commands.
        /// </summary>
        private string _configPath;

        private readonly IFileSystem _fileSystem;

        private KubectlContext()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _configPath = Path.GetTempFileName();

            // Add the environment variables to use to invoke kubectl safely. This environment is necessary
            // to ensure that the right credentials are used should the access token need to be refreshed.
            Environment[KubeConfigVariable] = _configPath;
            Environment[UseApplicationDefaultCredentialsVariable] = TrueValue;
            Environment[GoogleApplicationCredentialsVariable] = CredentialsPath;
            _fileSystem = GoogleCloudExtensionPackage.Instance.GetMefService<IFileSystem>();
        }

        /// <summary>
        /// Returns the <seealso cref="KubectlContext"/> instance to use for the given <paramref name="cluster"/> when
        /// performing Kubernetes operations.
        /// </summary>
        /// <param name="cluster">The name of the cluster for which to create credentials.</param>
        /// <param name="location">The name of the region or zone of the cluster.</param>
        /// <param name="clusterLocationType">The type of the cluster, either zonal or regional.</param>
        /// <returns>The <seealso cref="KubectlContext"/> for the given <paramref name="cluster"/>.</returns>
        /// <remarks>
        /// Do not use this method directly.
        /// Use <see cref="IKubectlContextProvider.GetKubectlContextForClusterAsync"/>.
        /// </remarks>
        public static async Task<KubectlContext> GetForClusterAsync(string cluster, string location, ClusterLocationType clusterLocationType)
        {
            var kubectlContext = new KubectlContext();
            if (!await kubectlContext.InitClusterCredentialsAsync(cluster, location, clusterLocationType))
            {
                throw new GCloudException($"Failed to get credentials for cluster {cluster}");
            }

            return kubectlContext;
        }

        private Task<bool> InitClusterCredentialsAsync(string cluster, string location, ClusterLocationType clusterLocationType)
        {
            string locationArg;
            switch (clusterLocationType)
            {
                case ClusterLocationType.Zone:
                    locationArg = $"--zone={location}";
                    break;
                case ClusterLocationType.Region:
                    locationArg = $"--region={location}";
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(Resources.UnexpectedMessageFormat, nameof(ClusterLocationType), clusterLocationType),
                        nameof(clusterLocationType));
            }
            string command = $"container clusters get-credentials {cluster} {locationArg}";
            return RunGcloudCommandAsync(command);
        }

        /// <summary>
        /// Creates a deployment for the given image and with the given name. The deployment is created with pods that
        /// contain a single container running <paramref name="imageTag"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to be created.</param>
        /// <param name="imageTag">The Docker image tag to use for the deployment.</param>
        /// <param name="replicas">The number of replicas in the deployment.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public async Task<bool> CreateDeploymentAsync(
            string name,
            string imageTag,
            int replicas,
            Func<string, OutputStream, Task> outputAction)
        {
            string command = $"run {name} --image={imageTag} --replicas={replicas} --port=8080 --record";
            return await RunKubectlCommandAsync(command, outputAction);
        }

        /// <summary>
        /// Exposes the service targeting the deployment <paramref name="deployment"/>. The ports being exposed are fixed
        /// to 80 for the service and 8080 for the target pods.
        /// </summary>
        /// <param name="deployment">The deployment for which to create and expose the service.</param>
        /// <param name="makePublic">True if the service should be made public, false otherwise.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public async Task<bool> ExposeServiceAsync(
            string deployment,
            bool makePublic,
            Func<string, OutputStream, Task> outputAction)
        {

            string type = makePublic ? "--type=LoadBalancer" : "--type=ClusterIP";
            string command = $"expose deployment {deployment} --port=80 --target-port=8080 {type}";
            return await RunKubectlCommandAsync(command, outputAction);
        }

        /// <summary>
        /// Returns the list of services running in the current cluster.
        /// </summary>
        /// <returns>The list of services.</returns>
        public async Task<IList<GkeService>> GetServicesAsync()
        {
            GkeList<GkeService> services = await GetKubectlCommandOutputAsync<GkeList<GkeService>>("get services");
            return services.Items;
        }

        /// <summary>
        /// Returns the service with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service to return.</param>
        /// <returns>The service.</returns>
        public Task<GkeService> GetServiceAsync(string name) => GetKubectlCommandOutputAsync<GkeService>($"get service {name} --ignore-not-found");

        /// <summary>
        /// Returns the list of deployments for the current cluster.
        /// </summary>
        /// <returns>The list of deployments.</returns>
        public async Task<IList<GkeDeployment>> GetDeploymentsAsync()
        {
            GkeList<GkeDeployment> deployments = await GetKubectlCommandOutputAsync<GkeList<GkeDeployment>>("get deployments");
            return deployments.Items;
        }

        /// <summary>
        /// Determines if a deployment with the given name already exists.
        /// </summary>
        /// <param name="name">The name of the deployment to check.</param>
        /// <returns>True if the deployment exists, false otherwise.</returns>
        public async Task<bool> DeploymentExistsAsync(string name)
        {
            IList<GkeDeployment> deployments = await GetDeploymentsAsync();
            return deployments.FirstOrDefault(x => x.Metadata.Name == name) != null;
        }

        /// <summary>
        /// Updates an existing deployment given by <paramref name="name"/> with <paramref name="imageTag"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to update.</param>
        /// <param name="imageTag">The Docker image tag to update to.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public async Task<bool> UpdateDeploymentImageAsync(
            string name,
            string imageTag,
            Func<string, OutputStream, Task> outputAction)
        {
            string command = $"set image deployment/{name} {name}={imageTag} --record";
            return await RunKubectlCommandAsync(command, outputAction);
        }

        /// <summary>
        /// Changes the number of replicas for the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to update.</param>
        /// <param name="replicas">The new number of replicas.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public async Task<bool> ScaleDeploymentAsync(string name, int replicas, Func<string, OutputStream, Task> outputAction)
        {
            string command = $"scale deployment {name} --replicas={replicas}";
            return await RunKubectlCommandAsync(command, outputAction);
        }

        /// <summary>
        /// Deletes the service given by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service to delete.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public async Task<bool> DeleteServiceAsync(string name, Func<string, OutputStream, Task> outputAction) =>
            await RunKubectlCommandAsync($"delete service {name}", outputAction);

        /// <summary>
        /// Gets the cluster IP address of a service.
        /// </summary>
        /// <param name="name">The name of the service to get the cluster IP address of.</param>
        /// <returns>The cluster IP address of the service.</returns>
        public async Task<string> GetServiceClusterIpAsync(string name)
        {
            GkeService service = await GetServiceAsync(name);
            return service?.Spec?.ClusterIp;
        }

        /// <summary>
        /// Gets the public IP address of a service.
        /// </summary>
        /// <param name="name">The name of the service to get the public IP address for.</param>
        /// <returns>The public IP address of the service.</returns>
        public async Task<string> GetPublicServiceIpAsync(string name)
        {
            GkeService service = await GetServiceAsync(name);
            return service?.Status?.LoadBalancer?.Ingress?.Select(i => i?.Ip).FirstOrDefault(ip => ip != null);
        }

        private async Task<bool> RunKubectlCommandAsync(string command, Func<string, OutputStream, Task> outputAction)
        {
            string actualCommand = FormatKubectlCommand(command);
            Debug.WriteLine($"Executing kubectl command: kubectl {actualCommand}");

            return await ProcessUtils.Default.RunCommandAsync(
                "kubectl",
                actualCommand,
                outputAction,
                environment: Environment);
        }

        private async Task<T> GetKubectlCommandOutputAsync<T>(string command)
        {
            string actualCommand = FormatKubectlOutputCommand(command);
            try
            {
                Debug.WriteLine($"Executing kubectl command: kubectl {actualCommand}");
                return await ProcessUtils.Default.GetJsonOutputAsync<T>(
                    "kubectl",
                    actualCommand,
                    environment: Environment);
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException(
                    $"Failed to exectue command {actualCommand}\nInner exception: {ex.Message}",
                    ex);
            }
        }

        private string FormatKubectlCommand(string command) => $"{command} --kubeconfig=\"{_configPath}\"";

        private string FormatKubectlOutputCommand(string command) => $"{FormatKubectlCommand(command)} --output=json";

        private void ReleaseUnmanagedResources()
        {
            if (_configPath == null)
            {
                return;
            }

            try
            {
                _fileSystem.File.Delete(_configPath);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to delete {_configPath}: {ex.Message}");
            }
            finally
            {
                _configPath = null;
            }
        }

        /// <summary>
        /// Deletes backing temporary the kubectl configuration files.
        /// </summary>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Deletes backing temporary the kubectl configuration files.
        /// </summary>
        ~KubectlContext()
        {
            ReleaseUnmanagedResources();
        }
    }
}