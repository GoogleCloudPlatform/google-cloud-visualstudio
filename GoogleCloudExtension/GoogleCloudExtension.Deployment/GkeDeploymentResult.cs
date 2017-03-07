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

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains the result of a GKE deployment.
    /// </summary>
    public class GkeDeploymentResult
    {
        /// <summary>
        /// The IP address of the public service if one was exposed. This property can be null
        /// if no public service was exposed or if there was a timeout trying to obtain the public
        /// IP address.
        /// </summary>
        public string PublicServiceIpAddress { get; }

        /// <summary>
        /// The IP address within the cluster for the service. This property can only be null if there
        /// was an error deploying the app.
        /// </summary>
        public string ClusterServiceIpAddress { get; }

        /// <summary>
        /// Is true if the a service was exposed publicly.
        /// </summary>
        public bool ServiceExposed { get; }

        /// <summary>
        /// Is true if the service was updated.
        /// </summary>
        public bool ServiceUpdated { get; }

        /// <summary>
        /// is true if the service was deleted.
        /// </summary>
        public bool ServiceDeleted { get; }

        /// <summary>
        /// Is true if the deployment was updated, false if a new deployment was created.
        /// </summary>
        public bool DeploymentUpdated { get; }

        /// <summary>
        /// Is true if the deployment number of replicas was changed.
        /// </summary>
        public bool DeploymentScaled { get; }

        public GkeDeploymentResult(
            string publicIpAddress,
            string privateIpAddress,
            bool serviceExposed,
            bool serviceUpdated,
            bool serviceDeleted,
            bool deploymentUpdated,
            bool deploymentScaled)
        {
            PublicServiceIpAddress = publicIpAddress;
            ClusterServiceIpAddress = privateIpAddress;
            ServiceExposed = serviceExposed;
            ServiceUpdated = serviceUpdated;
            ServiceDeleted = serviceDeleted;
            DeploymentUpdated = deploymentUpdated;
            DeploymentScaled = deploymentScaled;
        }
    }
}
