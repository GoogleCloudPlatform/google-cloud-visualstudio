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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about GCE instances and keeps track of operations in flight.
    /// </summary>
    public class GceDataSource : DataSourceBase<ComputeService>
    {
        /// <summary>
        /// This list is a global list of all of the operations pending created by instances of this class.
        /// It is not synchronized, as it is assumed that it will always be access from the same thread
        /// typically the UI thread.
        /// </summary>
        private static readonly List<GceOperation> s_pendingOperations = new List<GceOperation>();

        /// <summary>
        /// Initializes an instance of the data source.
        /// </summary>
        /// <param name="projectId">The project id that contains the GCE instances to manipulate.</param>
        /// <param name="credential">The credentials to use for the call.</param>
        public GceDataSource(string projectId, GoogleCredential credential) : base(projectId, CreateService(credential))
        { }

        private static ComputeService CreateService(GoogleCredential credential)
        {
            return new ComputeService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
            });
        }

        /// <summary>
        /// Returns the list of instances in all the zones for the project.
        /// </summary>
        public async Task<IList<Instance>> GetInstanceListAsync()
        {
            try
            {
                // 1) Request the list of zones for this project.
                var zones = await GetZoneListAsync();

                //  2) Request in parallel the instances in each zone.
                var requestResults = zones
                    .Select(x => GetInstancesInZoneListAsync(x.Name));

                // Flatten to single list.
                var results = await Task.WhenAll(requestResults);
                return results.SelectMany(x => x).ToList();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to call api: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Returns information about the given instance.
        /// </summary>
        /// <param name="zoneName">The zone in which the instance lives.</param>
        /// <param name="name">The name of the instance,</param>
        public async Task<Instance> GetInstance(string zoneName, string name)
        {
            try
            {
                return await Service.Instances.Get(ProjectId, zoneName, name).ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to call api: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Given an instance already fetched, reload it's data and return a new instance with the fresh data.
        /// </summary>
        /// <returns>The fresh instance.</returns>
        public Task<Instance> RefreshInstance(Instance instance) =>
            GetInstance(
                zoneName: instance.GetZoneName(),
                name: instance.Name);

        /// <summary>
        /// Returns the pending operation for the given instance if it exists.
        /// </summary>
        /// <param name="projectId">The project id that owns the instance.</param>
        /// <param name="zoneName">The zone name where the instance is located.</param>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The pending operation.</returns>
        public GceOperation GetPendingOperation(string projectId, string zoneName, string name) =>
            s_pendingOperations
                .Where(x => !x.OperationTask.IsCompleted)
                .FirstOrDefault(x => x.ProjectId == projectId && x.ZoneName == zoneName && x.Name == name);

        /// <summary>
        /// Looks up a pending operation for the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance for which to look an operation, it is assumed that the instance is in the same project as the current project.</param>
        /// <returns>The pending operation.</returns>
        public GceOperation GetPendingOperation(Instance instance) =>
            GetPendingOperation(projectId: ProjectId, zoneName: instance.GetZoneName(), name: instance.Name);

        /// <summary>
        /// Stops an instance in the current project.
        /// </summary>
        /// <param name="instance">The instance to stop.</param>
        /// <returns>The new operation in flight.</returns>
        public GceOperation StopInstance(Instance instance)
        {
            return StopInstance(zoneName: instance.GetZoneName(), name: instance.Name);
        }

        /// <summary>
        /// Stops an instance in the current project, given its <paramref name="zoneName"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="zoneName">The zone the instance is located.</param>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The new operation in flight.</returns>
        public GceOperation StopInstance(string zoneName, string name)
        {
            var operation = new GceOperation(
                          operationType: OperationType.StopInstance,
                          projectId: ProjectId,
                          zoneName: zoneName,
                          name: name);
            operation.OperationTask = StopInstanceImplAsync(operation, zoneName, name);
            return operation;
        }

        /// <summary>
        /// Stores the given operation in the pending operations list as the operation for this API call
        /// and performs the API call to stop the instance.
        /// </summary>
        /// <param name="pendingOperation">The operation to use for this API call.</param>
        /// <param name="zoneName">The zone where the instance is localed.</param>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The task that will be completed when the API call is complete, including the cleanup.</returns>
        private async Task StopInstanceImplAsync(GceOperation pendingOperation, string zoneName, string name)
        {
            try
            {
                var operation = await Service.Instances.Stop(ProjectId, zoneName, name).ExecuteAsync();
                s_pendingOperations.Add(pendingOperation);
                await WaitAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to call api: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            finally
            {
                s_pendingOperations.Remove(pendingOperation);
            }
        }

        /// <summary>
        /// Starts an instance in the given project.
        /// </summary>
        /// <param name="instance">The instance to start.</param>
        /// <returns>The new pending operation.</returns>
        public GceOperation StartInstance(Instance instance)
        {
            return StartInstance(zoneName: instance.GetZoneName(), name: instance.Name);
        }

        /// <summary>
        ///  Starts an instance in the current project, given its <paramref name="zoneName"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="zoneName">The zone where the instnace is lcoated.</param>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The new pending operation.</returns>
        public GceOperation StartInstance(string zoneName, string name)
        {
            var operation = new GceOperation(
                operationType: OperationType.StartInstance,
                projectId: ProjectId,
                zoneName: zoneName,
                name: name);
            operation.OperationTask = StartInstanceImplAsync(operation, zoneName, name);
            return operation;
        }

        /// <summary>
        /// Stores the given operation in the pending operations list as the operation for this API call
        /// and performs the API call to stop the instance.
        /// </summary>
        /// <param name="pendingOperation">The operation to use for this API call.</param>
        /// <param name="zoneName">The zone where the instance is localed.</param>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The task that will be completed when the API call is complete, including the cleanup.</returns>
        private async Task StartInstanceImplAsync(GceOperation pendingOperation, string zoneName, string name)
        {
            try
            {
                var operation = await Service.Instances.Start(ProjectId, zoneName, name).ExecuteAsync();
                s_pendingOperations.Add(pendingOperation);
                await WaitAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to call api: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            finally
            {
                s_pendingOperations.Remove(pendingOperation);
            }
        }

        private Task<IList<Zone>> GetZoneListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine("Fetching the first page.");
                        return Service.Zones.List(ProjectId).ExecuteAsync();
                    }
                    else
                    {
                        Debug.WriteLine($"Fetching page: {token}");
                        var request = Service.Zones.List(ProjectId);
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Items,
                x => x.NextPageToken);
        }

        private Task<IList<Instance>> GetInstancesInZoneListAsync(string zoneName)
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine("Fetching first page.");
                        return Service.Instances.List(ProjectId, zoneName).ExecuteAsync();
                    }
                    else
                    {
                        Debug.WriteLine($"Fetching page: {token}");
                        var request = Service.Instances.List(ProjectId, zoneName);
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Items,
                x => x.NextPageToken);
        }

        private async Task WaitAsync(Operation operation)
        {
            try
            {
                Debug.WriteLine($"Waiting on operation {operation.Name}");
                var zoneName = new Uri(operation.Zone).Segments.Last();
                while (true)
                {
                    var newOperation = await Service.ZoneOperations.Get(ProjectId, zoneName, operation.Name).ExecuteAsync();
                    if (newOperation.Status == "DONE")
                    {
                        if (newOperation.Error != null)
                        {
                            throw new DataSourceException($"Operation {operation.Name} failed.");
                        }
                        return;
                    }
                    await Task.Delay(2000);
                }
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to read operation: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
