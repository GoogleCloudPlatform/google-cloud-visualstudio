// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class contains all of the functionality to manage GCE instances.
    /// </summary>
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
        /// <returns>A task representing the asynchronous operation.</returns>
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
