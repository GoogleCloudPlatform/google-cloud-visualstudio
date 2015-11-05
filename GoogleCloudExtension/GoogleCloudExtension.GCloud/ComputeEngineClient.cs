using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    public static class ComputeEngineClient
    {
        /// <summary>
        /// Returns the list of compute instances for this class' notion of current
        /// user and project.
        /// </summary>
        /// <returns>The list of compute instances.</returns>
        public static Task<IList<ComputeInstance>> GetComputeInstanceListAsync()
        {
            return GCloudWrapper.Instance.GetJsonOutputAsync<IList<ComputeInstance>>("compute instances list");
        }

        /// <summary>
        /// Starts the GCE instance given its name and zone.
        /// </summary>
        /// <param name="name">The name of the GCE instance.</param>
        /// <param name="zone">The zone where the GCE instance resides.</param>
        /// <returns>The task.</returns>
        public static async Task StartComputeInstanceAsync(string name, string zone)
        {
            await GCloudWrapper.Instance.GetCommandOutputAsync($"compute instances start {name} --zone={zone}");
        }

        /// <summary>
        /// Stops the GCE instance given its name and zone.
        /// </summary>
        /// <param name="name">The name of the GCE instance.</param>
        /// <param name="zone">The zone where the GCE instance resides.</param>
        /// <returns></returns>
        public static async Task StopComputeInstanceAsync(string name, string zone)
        {
            await GCloudWrapper.Instance.GetCommandOutputAsync($"compute instances stop {name} --zone={zone}");
        }
    }
}
