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

using Google.Apis.Compute.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This interface defines a generic GCE data source.
    /// </summary>
    public interface IGceDataSource
    {
        /// <summary>
        /// Returns the list of instances in all the zones for the project.
        /// </summary>
        Task<IList<Instance>> GetInstanceListAsync();


        /// <summary>
        /// Returns all of the zones, and the instances within the zone, for the project.
        /// </summary>
        Task<IList<InstancesPerZone>> GetAllInstancesPerZonesAsync();

        /// <summary>
        /// Returns information about the given instance.
        /// </summary>
        /// <param name="zoneName">The zone in which the instance lives.</param>
        /// <param name="name">The name of the instance,</param>
        Task<Instance> GetInstance(string zoneName, string name);

        /// <summary>
        /// Given an instance already fetched, reload its data and return a new instance with the fresh data.
        /// </summary>
        /// <returns>The fresh instance.</returns>
        Task<Instance> RefreshInstance(Instance instance);

        /// <summary>
        /// Looks up a pending operation for the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance for which to look an operation, it is assumed that the instance is in the same project as the current project.</param>
        /// <returns>The pending operation.</returns>
        GceOperation GetPendingOperation(Instance instance);

        /// <summary>
        /// Stops an instance in the current project.
        /// </summary>
        /// <param name="instance">The instance to stop.</param>
        /// <returns>The new operation in flight.</returns>
        GceOperation StopInstance(Instance instance);

        /// <summary>
        /// Stops an instance in the current project, given its <paramref name="zoneName"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="zoneName">The zone the instance is located.</param>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The new operation in flight.</returns>
        GceOperation StopInstance(string zoneName, string name);

        /// <summary>
        /// Starts an instance in the given project.
        /// </summary>
        /// <param name="instance">The instance to start.</param>
        /// <returns>The new pending operation.</returns>
        GceOperation StartInstance(Instance instance);

        /// <summary>
        ///  Starts an instance in the current project, given its <paramref name="zoneName"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="zoneName">The zone where the instance is located.</param>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The new pending operation.</returns>
        GceOperation StartInstance(string zoneName, string name);

        /// <summary>
        /// Updates the instance ports, enables the <paramref name="portsToEnable"/> and disables the
        /// ports in <paramref name="portsToDisable"/>.
        /// </summary>
        /// <param name="instance">The instance to modify.</param>
        /// <param name="portsToEnable">The list of ports to enable.</param>
        /// <param name="portsToDisable">The list of ports to disable.</param>
        /// <returns>The operation.</returns>
        GceOperation UpdateInstancePorts(
            Instance instance,
            IList<FirewallPort> portsToEnable,
            IList<FirewallPort> portsToDisable);

        /// <summary>
        /// Returns the list of all firewall rules for the current project.
        /// </summary>
        Task<IList<Firewall>> GetFirewallListAsync();
    }
}
