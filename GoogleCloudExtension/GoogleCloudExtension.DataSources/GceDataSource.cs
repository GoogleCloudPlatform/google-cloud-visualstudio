// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

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
    /// Data source that returns information about GCE instances. Calls the GCE API according 
    /// to https://cloud.google.com/compute/docs/reference/latest/.
    /// </summary>
    public class GceDataSource : DataSourceBase<ComputeService>
    {
        private static readonly List<GceOperation> s_pendingOperations = new List<GceOperation>();

        public GceDataSource(string projectId, GoogleCredential credential) : base(projectId, () => CreateService(credential))
        { }

        private static ComputeService CreateService(GoogleCredential credential)
        {
            return new ComputeService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
            });
        }

        /// <summary>
        /// Returns the list of instances for the given <paramref name="projectId"/>.
        /// </summary>
        /// <param name="projectId">The project id that contains the instances.</param>
        /// <param name="credential">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
        public async Task<IList<Instance>> GetInstanceListAsync()
        {
            try
            {
                // 1) Request the list of zones for this project.
                var zones = await GetZoneListAsync();

                //  2) Request in parallel the instances in each zone.
                var result = new List<Instance>();
                var requestResults = zones
                    .Select(x => GetInstancesInZoneListAsync(x.Name));

                // 3) Merge the results into a single list.
                foreach (var instancesPerZone in await Task.WhenAll(requestResults))
                {
                    result.AddRange(instancesPerZone);
                }
                return result;
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
        /// <param name="projectId">The project id that contains the instance.</param>
        /// <param name="zoneName">The zone in which the instance lives.</param>
        /// <param name="name">The name of the instance,</param>
        /// <param name="oauthToken">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
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
        /// <returns></returns>
        public Task<Instance> RefreshInstance(Instance instance) => GetInstance(
            zoneName: instance.ZoneName(),
            name: instance.Name);

        public static IEnumerable<GceOperation> GetPendingOperations() => s_pendingOperations;

        public GceOperation GetPendingOperation(string projectId, string zoneName, string name) =>
            s_pendingOperations
            .Where(x => !x.OperationTask.IsCompleted)
            .FirstOrDefault(x => x.ProjectId == projectId && x.ZoneName == zoneName && x.Name == name);

        public GceOperation GetPendingOperation(Instance instance) => 
            GetPendingOperation(projectId: ProjectId, zoneName: instance.ZoneName(), name: instance.Name);

        public GceOperation StopInstance(Instance instance)
        {
            return StopInstance(zoneName: instance.ZoneName(), name: instance.Name);
        }

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

        private async Task StopInstanceImplAsync(GceOperation pendingOperation, string zoneName, string name)
        {
            try
            {
                var operation = await Service.Instances.Stop(ProjectId, zoneName, name).ExecuteAsync();
                s_pendingOperations.Add(pendingOperation);
                await operation.NewWait(Service, ProjectId);
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

        public GceOperation StartInstance(Instance instance)
        {
            return StartInstance(zoneName: instance.ZoneName(), name: instance.Name);
        }

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

        private async Task StartInstanceImplAsync(GceOperation pendingOperation, string zoneName, string name)
        {
            try
            {
                var operation = await Service.Instances.Start(ProjectId, zoneName, name).ExecuteAsync();
                s_pendingOperations.Add(pendingOperation);
                await operation.NewWait(Service, ProjectId);
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
        /// Fetches the list of zones for the given project.
        /// </summary>
        /// <param name="projectId">The project id for which to fetch the zone data.</param>
        /// <param name="oauthToken">The auth token to use to authenticate this call.</param>
        /// <returns></returns>
        private Task<IList<Zone>> GetZoneListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine("Fetching the last page.");
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

        /// <summary>
        /// Fetches the list of instances in the given zone and project.
        /// </summary>
        private Task<IList<Instance>> GetInstancesInZoneListAsync(string zoneName)
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine("Fetching last page.");
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
    }
}
