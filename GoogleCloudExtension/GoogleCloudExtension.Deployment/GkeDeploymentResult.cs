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

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains the result of a GKE deployment.
    /// </summary>
    public class GkeDeploymentResult
    {
        /// <summary>
        /// The IP address of the public service if one was exposed. This property will be null if:
        ///   * There was a timeout while waiting for the service to go up.
        ///   * No service was exposed.
        /// </summary>
        public string ServiceIpAddress { get; }

        /// <summary>
        /// Is true if the a service was exposed publicly.
        /// </summary>
        public bool WasExposed { get; }

        /// <summary>
        /// Is true if the deployment was updated, false if a new deployment was created.
        /// </summary>
        public bool DeploymentUpdated { get; }

        public GkeDeploymentResult(string serviceIpAddress, bool wasExposed, bool deploymentUpdated)
        {
            ServiceIpAddress = serviceIpAddress;
            WasExposed = wasExposed;
            DeploymentUpdated = deploymentUpdated;
        }
    }
}
