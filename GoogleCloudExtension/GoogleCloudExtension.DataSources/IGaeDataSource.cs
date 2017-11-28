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

using Google.Apis.Appengine.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This interface represents a generic App Engine data source.
    /// </summary>
    public interface IGaeDataSource
    {
        /// <summary>
        /// Fetches the the GAE application for the given project.
        /// </summary>
        /// <returns>The GAE application.</returns>
        Task<Application> GetApplicationAsync();

        /// <summary>
        /// Fetches the list of GAE services for the given project.
        /// </summary>
        /// <returns>The list of GAE services.</returns>
        Task<IList<Service>> GetServiceListAsync();

        /// <summary>
        /// Gets a GAE service for the given project and service id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>The GAE service.</returns>
        Task<Service> GetServiceAsync(string serviceId);

        /// <summary>
        /// Deletes a GAE service for the given project and service id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>A taks that will be completed once the operation finishes.</returns>
        Task DeleteServiceAsync(string serviceId);

        /// <summary>
        /// Update a GAE services's traffic split.
        /// </summary>
        /// <param name="split">The traffic split to set.</param>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>A task that will be comleted once the operation is finished.</returns>
        Task UpdateServiceTrafficSplitAsync(TrafficSplit split, string serviceId);

        /// <summary>
        /// Fetches the list of GAE versions for the given project and service.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>The list of GAE versions.</returns>
        Task<IList<Version>> GetVersionListAsync(string serviceId);

        /// <summary>
        /// Gets a GAE version for the given project, service and version id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>The GAE version.</returns>
        Task<Version> GetVersionAsync(string serviceId, string versionId);

        /// <summary>
        /// Deletes a GAE version for the given project, service and version id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>A task that will be completed once the oepration is finished.</returns>
        Task DeleteVersionAsync(string serviceId, string versionId);

        /// <summary>
        /// Update a GAE version's serving status.
        /// </summary>
        /// <param name="status">The serving status to set.  Either 'SERVING' or 'STOPPED'</param>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>A task that will be completed once the operation is finished.</returns>
        Task UpdateVersionServingStatus(string status, string serviceId, string versionId);

        /// <summary>
        /// Fetches the list of GAE instance for the given project, service and version.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>The list of GAE versions.</returns>
        Task<IList<Instance>> GetInstanceListAsync(string serviceId, string versionId);

        /// <summary>
        /// Returns the list of locations that have Flex enabled.
        /// </summary>
        Task<IList<string>> GetFlexLocationsAsync();

        /// <summary>
        /// Returns the list of available locations for the current project.
        /// </summary>
        Task<IList<Location>> GetAvailableLocationsAsync();

        /// <summary>
        /// Creates an application in the given location.
        /// </summary>
        /// <param name="location">The location where to create the app.</param>
        /// <returns>A task that will be completed once the operation finishes.</returns>
        Task CreateApplicationAsync(string location);
    }
}
