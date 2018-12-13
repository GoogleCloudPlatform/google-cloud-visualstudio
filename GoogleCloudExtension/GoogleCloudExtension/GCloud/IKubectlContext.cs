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
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    public interface IKubectlContext : IGCloudContext, IDisposable
    {
        /// <summary>
        /// Creates a deployment for the given image and with the given name. The deployment is created with pods that
        /// contain a single container running <paramref name="imageTag"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to be created.</param>
        /// <param name="imageTag">The Docker image tag to use for the deployment.</param>
        /// <param name="replicas">The number of replicas in the deployment.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        Task<bool> CreateDeploymentAsync(
            string name,
            string imageTag,
            int replicas,
            Func<string, OutputStream, Task> outputAction);

        /// <summary>
        /// Exposes the service targeting the deployment <paramref name="deployment"/>. The ports being exposed are fixed
        /// to 80 for the service and 8080 for the target pods.
        /// </summary>
        /// <param name="deployment">The deployment for which to create and expose the service.</param>
        /// <param name="makePublic">True if the service should be made public, false otherwise.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        Task<bool> ExposeServiceAsync(string deployment, bool makePublic, Func<string, OutputStream, Task> outputAction);

        /// <summary>
        /// Returns the service with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service to return.</param>
        /// <returns>The service.</returns>
        Task<GkeService> GetServiceAsync(string name);

        /// <summary>
        /// Returns the list of deployments for the current cluster.
        /// </summary>
        /// <returns>The list of deployments.</returns>
        Task<IList<GkeDeployment>> GetDeploymentsAsync();

        /// <summary>
        /// Updates an existing deployment given by <paramref name="name"/> with <paramref name="imageTag"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to update.</param>
        /// <param name="imageTag">The Docker image tag to update to.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        Task<bool> UpdateDeploymentImageAsync(
            string name,
            string imageTag,
            Func<string, OutputStream, Task> outputAction);

        /// <summary>
        /// Changes the number of replicas for the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the deployment to update.</param>
        /// <param name="replicas">The new number of replicas.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        Task<bool> ScaleDeploymentAsync(string name, int replicas, Func<string, OutputStream, Task> outputAction);

        /// <summary>
        /// Deletes the service given by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service to delete.</param>
        /// <param name="outputAction">The output callback to be called with output from the command.</param>
        /// <returns>True if the operation succeeded false otherwise.</returns>
        Task<bool> DeleteServiceAsync(string name, Func<string, OutputStream, Task> outputAction);

        /// <summary>
        /// Gets the cluster IP address of a service.
        /// </summary>
        /// <param name="name">The name of the service to get the cluster IP address of.</param>
        /// <returns>The cluster IP address of the service.</returns>
        Task<string> GetServiceClusterIpAsync(string name);

        /// <summary>
        /// Gets the public IP address of a service.
        /// </summary>
        /// <param name="name">The name of the service to get the public IP address for.</param>
        /// <returns>The public IP address of the service.</returns>
        Task<string> GetPublicServiceIpAsync(string name);
    }
}