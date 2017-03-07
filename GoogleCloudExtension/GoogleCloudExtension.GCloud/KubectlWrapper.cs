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

using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class contains methods that wrap the functionality implemented by kubectl into something
    /// that can be called from the extension.
    /// </summary>
    public static class KubectlWrapper
    {
        /// <summary>
        /// Creates a deployment for the given image and with the given name. The deployment is created with pods that
        /// contain a single container running <paramref name="imageTag"/>.
        /// </summary>
        /// <param name="name">The name of the deployemnt to be created.</param>
        /// <param name="imageTag">The Docker image tag to use for the deployment.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public static Task<bool> CreateDeploymentAsync(
            string name,
            string imageTag,
            int replicas,
            Action<string> outputAction,
            KubectlContext context)
        {
            return RunCommandAsync($"run {name} --image={imageTag} --replicas={replicas} --port=8080 --record", outputAction, context);
        }

        /// <summary>
        /// Exposes the service targetting the deployemnt <paramref name="deployment"/>. The ports being exposed are fixed
        /// to 80 for the service and 8080 for the target pods.
        /// </summary>
        /// <param name="deployment">The deployment for which to create and expose the service.</param>
        /// <param name="makePublic">True if the service should be made public, false otherwise.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public static Task<bool> ExposeServiceAsync(
            string deployment,
            bool makePublic,
            Action<string> outputAction,
            KubectlContext context)
        {
            var type = makePublic ? "--type=LoadBalancer" : "--type=ClusterIP";
            return RunCommandAsync(
                $"expose deployment {deployment} --port=80 --target-port=8080 {type}",
                outputAction,
                context);
        }

        /// <summary>
        /// Returns the list of services running in the current cluster.
        /// </summary>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>The list of services.</returns>
        public static async Task<IList<GkeService>> GetServicesAsync(KubectlContext context)
        {
            var services = await GetJsonOutputAsync<GkeList<GkeService>>("get services", context);
            return services.Items;
        }

        /// <summary>
        /// Returns the service with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service to return.</param>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>The service.</returns>
        public static Task<GkeService> GetServiceAsync(string name, KubectlContext context)
        {
            return GetJsonOutputAsync<GkeService>($"get service {name}", context);
        }

        /// <summary>
        /// Returns the list of deployments for the current cluster.
        /// </summary>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>The list of deployments.</returns>
        public static async Task<IList<GkeDeployment>> GetDeploymentsAsync(KubectlContext context)
        {
            var deployments = await GetJsonOutputAsync<GkeList<GkeDeployment>>($"get deployments", context);
            return deployments.Items;
        }

        /// <summary>
        /// Determines if a deployment with the given name already exists.
        /// </summary>
        /// <param name="name">The name of the deployment to check.</param>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>True if the deployment exists, false otherwise.</returns>
        public static async Task<bool> DeploymentExistsAsync(string name, KubectlContext context)
        {
            var deployments = await GetDeploymentsAsync(context);
            return deployments.FirstOrDefault(x => x.Metadata.Name == name) != null;
        }

        /// <summary>
        /// Updates an existing deployemnt given by <paramref name="name"/> with <paramref name="imageTag"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to update.</param>
        /// <param name="imageTag">The Docker image tag to update to.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public static Task<bool> UpdateDeploymentImageAsync(
            string name,
            string imageTag,
            Action<string> outputAction,
            KubectlContext context)
        {
            return RunCommandAsync(
                $"set image deployment/{name} {name}={imageTag} --record",
                outputAction,
                context);
        }

        /// <summary>
        /// Changes the number of replicas for the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to update.</param>
        /// <param name="replicas">The new number of replicas.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public static Task<bool> ScaleDeploymentAsync(
            string name,
            int replicas,
            Action<string> outputAction,
            KubectlContext context)
        {
            return RunCommandAsync(
                $"scale deployment {name} --replicas={replicas}",
                outputAction,
                context);
        }

        /// <summary>
        /// Deletes the service given by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service to delete.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <param name="context">The context for invoking kubectl.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        public static Task<bool> DeleteServiceAsync(string name, Action<string> outputAction, KubectlContext context)
            => RunCommandAsync($"delete service {name}", outputAction, context);

        private static Task<bool> RunCommandAsync(string command, Action<string> outputAction, KubectlContext context)
        {
            var actualCommand = FormatCommand(command, context);
            Debug.WriteLine($"Executing kubectl command: kubectl {actualCommand}");
            Dictionary<string, string> environment = GetEnvironmentForContext(context);

            return ProcessUtils.RunCommandAsync(
                "kubectl",
                actualCommand,
                (o, e) => outputAction(e.Line),
                environment: environment);
        }

        private static async Task<T> GetJsonOutputAsync<T>(string command, KubectlContext context)
        {
            var actualCommand = FormatCommand(command, context, jsonOutput: true);
            try
            {
                Debug.WriteLine($"Executing kubectl command: kubectl {actualCommand}");
                var environment = GetEnvironmentForContext(context);
                return await ProcessUtils.GetJsonOutputAsync<T>("kubectl", actualCommand, environment: environment);
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to exectue command {actualCommand}\nInner exception: {ex.Message}", ex);
            }
        }

        private static string FormatCommand(string command, KubectlContext context, bool jsonOutput = false)
        {
            var format = jsonOutput ? "--output=json" : "";

            return $"{command} --kubeconfig=\"{context.ConfigPath}\" {format}";
        }

        /// <summary>
        /// Returns the environment variables to use to invoke kubectl safely. This environemnt is necessary
        /// to ensure that the right credentials are used should the access token need to be refreshed.
        /// </summary>
        private static Dictionary<string, string> GetEnvironmentForContext(KubectlContext context)
        {
            return new Dictionary<string, string>
            {
                [CommonEnvironmentVariables.GCloudContainerUseApplicationDefaultCredentialsVariable] = CommonEnvironmentVariables.TrueValue,
                [CommonEnvironmentVariables.GoogleApplicationCredentialsVariable] = context.CredentialsPath
            };
        }
    }
}
