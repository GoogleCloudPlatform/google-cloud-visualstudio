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
    public class GceDataSource : DataSourceBase<ComputeService>, IGceDataSource
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
        /// <param name="appName">The name of the application.</param>
        public GceDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new ComputeService(init), appName)
        { }

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
        /// Returns all of the zones, and the instances within the zone, for the project.
        /// </summary>
        public async Task<IList<InstancesPerZone>> GetAllInstancesPerZonesAsync()
        {
            try
            {
                // 1) Request the list of zones for this project.
                var zones = await GetZoneListAsync();

                //  2) Request in parallel the instances in each zone.
                var requestResults = zones
                    .Select(async (x) => new InstancesPerZone(await GetInstancesInZoneListAsync(x.Name), x));

                return await Task.WhenAll(requestResults);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get instances and zones: {ex.Message}");
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
        /// Given an instance already fetched, reload its data and return a new instance with the fresh data.
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
        public static GceOperation GetPendingOperation(string projectId, string zoneName, string name) =>
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
        public GceOperation StopInstance(Instance instance) => StopInstance(zoneName: instance.GetZoneName(), name: instance.Name);

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
        private Task StopInstanceImplAsync(GceOperation pendingOperation, string zoneName, string name) =>
            RegisterPendingOperation(pendingOperation, () => Service.Instances.Stop(ProjectId, zoneName, name).ExecuteAsync());

        /// <summary>
        /// Starts an instance in the given project.
        /// </summary>
        /// <param name="instance">The instance to start.</param>
        /// <returns>The new pending operation.</returns>
        public GceOperation StartInstance(Instance instance) => StartInstance(zoneName: instance.GetZoneName(), name: instance.Name);

        /// <summary>
        ///  Starts an instance in the current project, given its <paramref name="zoneName"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="zoneName">The zone where the instance is located.</param>
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
        private Task StartInstanceImplAsync(GceOperation pendingOperation, string zoneName, string name) =>
            RegisterPendingOperation(pendingOperation, () => Service.Instances.Start(ProjectId, zoneName, name).ExecuteAsync());

        /// <summary>
        /// Returns the list of all firewall rules for the current project.
        /// </summary>
        public Task<IList<Firewall>> GetFirewallListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (token == null)
                    {
                        return Service.Firewalls.List(ProjectId).ExecuteAsync();
                    }
                    else
                    {
                        var request = Service.Firewalls.List(ProjectId);
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Items,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Creates a firewall rule with the given name that opens up the given port, targetting the very same
        /// name as the target tag. GCE instances with that tag will be affected by this rule.
        /// </summary>
        /// <param name="port">The port to open.</param>
        /// <returns>The task that will be fulfilled once the operation is completed.</returns>
        public async Task CreateFirewall(FirewallPort port)
        {
            try
            {
                var newFirewall = new Firewall
                {
                    Name = port.Name,
                    Allowed = EnablePort(port),
                    TargetTags = new List<string> { port.Name },
                };

                var operation = await Service.Firewalls.Insert(newFirewall, ProjectId).ExecuteAsync();
                await WaitAsync(operation);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to create firewall: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        private IList<Firewall.AllowedData> EnablePort(FirewallPort port)
        {
            var allowedData = new Firewall.AllowedData
            {
                IPProtocol = port.ProtocolString,
                Ports = new List<string> { port.Port.ToString() },
            };
            return new List<Firewall.AllowedData> { allowedData };
        }

        /// <summary>
        /// Sets the tags for a GCE instance to <paramref name="tags"/>. The task wrapped by the operation will throw
        /// if the list of tags was modified already, and the fingerprint for the tags doesn't match.
        /// </summary>
        /// <param name="instance">The instance to modify.</param>
        /// <param name="tags">The tags to set.</param>
        /// <returns>The operation.</returns>
        public GceOperation SetInstanceTags(Instance instance, IList<string> tags)
        {
            var operation = new GceOperation(
                operationType: OperationType.SettingTags,
                projectId: ProjectId,
                zoneName: instance.GetZoneName(),
                name: instance.Name);
            operation.OperationTask = SetInstanceTagsAsyncImpl(operation, instance, tags);
            return operation;
        }

        private Task SetInstanceTagsAsyncImpl(GceOperation pendingOperation, Instance instance, IList<string> tags) =>
            RegisterPendingOperation(pendingOperation, () =>
            {
                var newTags = new Tags
                {
                    Items = tags,
                    Fingerprint = instance.Tags.Fingerprint,
                };
                return Service.Instances.SetTags(newTags, ProjectId, instance.GetZoneName(), instance.Name).ExecuteAsync();
            });

        /// <summary>
        /// Updates the instance ports, enables the <paramref name="portsToEnable"/> and disables the
        /// ports in <paramref name="portsToDisable"/>.
        /// </summary>
        /// <param name="instance">The instance to modify.</param>
        /// <param name="portsToEnable">The list of ports to enable.</param>
        /// <param name="portsToDisable">The list of ports to disable.</param>
        /// <returns>The operation.</returns>
        public GceOperation UpdateInstancePorts(
            Instance instance,
            IList<FirewallPort> portsToEnable,
            IList<FirewallPort> portsToDisable)
        {
            var operation = new GceOperation(
                OperationType.ModifyingFirewall,
                ProjectId,
                instance.GetZoneName(),
                instance.Name);
            operation.OperationTask = UpdateInstancePortsAsyncImpl(operation, instance, portsToEnable, portsToDisable);
            return operation;
        }

        private async Task UpdateInstancePortsAsyncImpl(
            GceOperation pendingOperation,
            Instance instance,
            IList<FirewallPort> portsToEnable,
            IList<FirewallPort> portsToDisable)
        {
            try
            {
                s_pendingOperations.Add(pendingOperation);

                // 1) Ensure that the firewall rules for the ports to be enabled are present.
                await EnsureFirewallRules(portsToEnable);

                // 2) Update the tags for the instance.
                var tagsToAdd = portsToEnable.Select(x => x.Name);
                var tagsToRemove = portsToDisable.Select(x => x.Name);
                var instanceTags = instance.Tags?.Items ?? Enumerable.Empty<string>();
                var tagsOperation = SetInstanceTags(
                    instance,
                    instanceTags.Except(tagsToRemove).Union(tagsToAdd).ToList());
                await tagsOperation.OperationTask;
            }
            finally
            {
                s_pendingOperations.Remove(pendingOperation);
            }
        }

        private async Task EnsureFirewallRules(IEnumerable<FirewallPort> portsToEnable)
        {
            var firewalls = await GetFirewallListAsync();
            var firewallNames = firewalls.Select(x => x.Name).ToList();
            var tasks = from port in portsToEnable
                        where !firewallNames.Contains(port.Name)
                        select CreateFirewall(port);

            await Task.WhenAll(tasks);
        }

        private Task<IList<Zone>> GetZoneListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GceDataSource)}, {nameof(GetZoneListAsync)}: Fetching the first page.");
                        return Service.Zones.List(ProjectId).ExecuteAsync();
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GceDataSource)}, {nameof(GetZoneListAsync)}: Fetching page: {token}");
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
                        Debug.WriteLine($"{nameof(GceDataSource)}, {nameof(GetInstancesInZoneListAsync)}: Fetching first page.");
                        return Service.Instances.List(ProjectId, zoneName).ExecuteAsync();
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GceDataSource)}, {nameof(GetInstancesInZoneListAsync)}: Fetching page: {token}");
                        var request = Service.Instances.List(ProjectId, zoneName);
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Items,
                x => x.NextPageToken);
        }

        private async Task RegisterPendingOperation(GceOperation pendingOperation, Func<Task<Operation>> operationFactory)
        {
            try
            {
                var operation = await operationFactory();
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

        private Task WaitAsync(Operation operation)
        {
            string zoneName = null;

            if (operation.Zone != null)
            {
                zoneName = new Uri(operation.Zone).Segments.Last();
            }

            return operation.AwaitOperationAsync(
                refreshOperation: (op) =>
                {
                    if (zoneName != null)
                    {
                        return Service.ZoneOperations.Get(ProjectId, zoneName, op.Name).ExecuteAsync();
                    }
                    else
                    {
                        return Service.GlobalOperations.Get(ProjectId, op.Name).ExecuteAsync();
                    }
                },
                isFinished: op => op.Status == "DONE",
                getErrorData: op => op.Error,
                getErrorMessage: err => err.Errors.FirstOrDefault()?.Message);
        }
    }
}
