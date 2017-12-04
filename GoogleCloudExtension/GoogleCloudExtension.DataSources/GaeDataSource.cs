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

using Google;
using Google.Apis.Appengine.v1;
using Google.Apis.Appengine.v1.Data;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about GAE apps and keeps track of operations in flight.
    /// </summary>
    public class GaeDataSource : DataSourceBase<AppengineService>, IGaeDataSource
    {
        private static readonly string s_servingStatusUpdateMask = "servingStatus";
        private static readonly string s_trafficSplitUpdateMask = "split";

        // Timeout for polling for operations.
        private static readonly TimeSpan s_operationDefaultTimeout = new TimeSpan(0, 10, 0);

        /// <summary>
        /// Initializes an instance of the data source.
        /// </summary>
        /// <param name="projectId">The project id that contains the GAE instances to manipulate.</param>
        /// <param name="credential">The credentials to use for the call.</param>
        /// <param name="appName">The name of the application.</param>
        public GaeDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new AppengineService(init), appName)
        { }

        /// <summary>
        /// Fetches the the GAE application for the given project.
        /// </summary>
        /// <returns>The GAE application.</returns>
        public async Task<Application> GetApplicationAsync()
        {
            try
            {
                AppsResource.GetRequest request = Service.Apps.Get(ProjectId);
                return await request.ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                if (ex.Error.Code == 404)
                {
                    Debug.WriteLine("Failed to find application, not an error.");
                    return null;
                }

                Debug.WriteLine($"Failed to get application: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Fetches the list of GAE services for the given project.
        /// </summary>
        /// <returns>The list of GAE services.</returns>
        public async Task<IList<Service>> GetServiceListAsync()
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Apps.Services.List(ProjectId);
                    if (!String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetServiceListAsync)}: Fetching page: {token}");
                        request.PageToken = token;
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetServiceListAsync)}: Fetching first page.");
                    }
                    return request.ExecuteAsync();
                },
                x => x.Services,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Gets a GAE service for the given project and service id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>The GAE service.</returns>
        public async Task<Service> GetServiceAsync(string serviceId)
        {
            try
            {
                var request = Service.Apps.Services.Get(ProjectId, serviceId);
                return await request.ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get service: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deletes a GAE service for the given project and service id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>A taks that will be completed once the operation finishes.</returns>
        public async Task DeleteServiceAsync(string serviceId)
        {
            try
            {
                var request = Service.Apps.Services.Delete(ProjectId, serviceId);
                Operation operation = await request.ExecuteAsync();
                await AwaitOperationAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to delete service: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Timeout while deleting service: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Update a GAE services's traffic split.
        /// </summary>
        /// <param name="split">The traffic split to set.</param>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>A task that will be comleted once the operation is finished.</returns>
        public async Task UpdateServiceTrafficSplitAsync(TrafficSplit split, string serviceId)
        {
            try
            {
                // Create a service with just the traffic split set.
                Service service = new Service
                {
                    Split = split
                };
                var request = Service.Apps.Services.Patch(service, ProjectId, serviceId);
                // Only update the traffic split.
                request.UpdateMask = s_trafficSplitUpdateMask;
                Operation operation = await request.ExecuteAsync();
                await AwaitOperationAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to update traffic split: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Timeout while updating service: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Fetches the list of GAE versions for the given project and service.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>The list of GAE versions.</returns>
        public async Task<IList<Google.Apis.Appengine.v1.Data.Version>> GetVersionListAsync(string serviceId)
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Apps.Services.Versions.List(ProjectId, serviceId);
                    if (!String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetVersionListAsync)}: Fetching page: {token}");
                        request.PageToken = token;
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetVersionListAsync)}: Fetching first page.");
                    }
                    return request.ExecuteAsync();
                },
                x => x.Versions,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Gets a GAE version for the given project, service and version id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>The GAE version.</returns>
        public async Task<Google.Apis.Appengine.v1.Data.Version> GetVersionAsync(string serviceId, string versionId)
        {
            try
            {
                var request = Service.Apps.Services.Versions.Get(ProjectId, serviceId, versionId);
                return await request.ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get version: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deletes a GAE version for the given project, service and version id.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>A task that will be completed once the oepration is finished.</returns>
        public async Task DeleteVersionAsync(string serviceId, string versionId)
        {
            try
            {
                var request = Service.Apps.Services.Versions.Delete(ProjectId, serviceId, versionId);
                Operation operation = await request.ExecuteAsync();
                await AwaitOperationAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to delete version: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Timeout while deleting version: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Update a GAE version's serving status.
        /// </summary>
        /// <param name="status">The serving status to set.  Either 'SERVING' or 'STOPPED'</param>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>A task that will be completed once the operation is finished.</returns>
        public async Task UpdateVersionServingStatus(string status, string serviceId, string versionId)
        {
            try
            {
                // Create a version with just the service status set.
                Google.Apis.Appengine.v1.Data.Version version = new Google.Apis.Appengine.v1.Data.Version()
                {
                    ServingStatus = status,
                };
                var request = Service.Apps.Services.Versions.Patch(version, ProjectId, serviceId, versionId);
                // Only update the service status.
                request.UpdateMask = s_servingStatusUpdateMask;
                Operation operation = await request.ExecuteAsync();
                await AwaitOperationAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to update serving status to '{status}': {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Timeout while updating version: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Fetches the list of GAE instance for the given project, service and version.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>The list of GAE versions.</returns>
        public async Task<IList<Instance>> GetInstanceListAsync(string serviceId, string versionId)
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Apps.Services.Versions.Instances.List(ProjectId, serviceId, versionId);
                    if (!String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetInstanceListAsync)}: Fetching page: {token}");
                        request.PageToken = token;
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetInstanceListAsync)}: Fetching first page.");
                    }
                    return request.ExecuteAsync();
                },
                x => x.Instances,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Returns the list of locations that have Flex enabled.
        /// </summary>
        public async Task<IList<string>> GetFlexLocationsAsync()
        {
            IList<Location> availableLocations = await GetAvailableLocationsAsync();
            return availableLocations
                .Where(x => x.IsFlexEnabled())
                .Select(x => x.GetRegionName())
                .ToList();
        }

        /// <summary>
        /// Returns the list of available locations for the current project.
        /// </summary>
        public Task<IList<Location>> GetAvailableLocationsAsync()
        {
            var request = Service.Apps.Locations.List(ProjectId);

            return LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.Locations,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Creates an application in the given location.
        /// </summary>
        /// <param name="location">The location where to create the app.</param>
        /// <returns>A task that will be completed once the operation finishes.</returns>
        public async Task CreateApplicationAsync(string location)
        {
            var request = Service.Apps.Create(new Application
            {
                Id = ProjectId,
                LocationId = location
            });

            try
            {
                var operation = await request.ExecuteAsync();
                await AwaitOperationAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                throw new DataSourceException(ex.Message, ex);
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Timeout while creating app: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Awaits for the operation to complete succesfully.
        /// </summary>
        /// <param name="operation">The operation to await.</param>
        /// <returns>The task that will be done once the operation is succesful.</returns>
        private Task AwaitOperationAsync(Operation operation)
        {
            var tokenSource = new CancellationTokenSource(s_operationDefaultTimeout);

            return operation.AwaitOperationAsync(
                refreshOperation: op => Service.Apps.Operations.Get(ProjectId, GetOperationId(op)).ExecuteAsync(),
                isFinished: op => op.Done ?? false,
                getErrorData: op => op.Error,
                getErrorMessage: err => err.Message,
                token: tokenSource.Token);
        }

        private static string GetOperationId(Operation operation) => operation.Name.Split('/').Last();
    }
}
