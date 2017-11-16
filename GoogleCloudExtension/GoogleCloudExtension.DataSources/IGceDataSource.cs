using Google.Apis.Compute.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
