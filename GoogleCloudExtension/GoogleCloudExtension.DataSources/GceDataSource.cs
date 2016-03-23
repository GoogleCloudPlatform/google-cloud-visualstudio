// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about GCE instances. Calls the GCE API according 
    /// to https://cloud.google.com/compute/docs/reference/latest/.
    /// </summary>
    public static class GceDataSource
    {
        /// <summary>
        /// Returns the list of instances for the given <paramref name="projectId"/>.
        /// </summary>
        /// <param name="projectId">The project id that contains the instances.</param>
        /// <param name="oauthToken">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
        public static async Task<IList<GceInstance>> GetInstanceListAsync(string projectId, string oauthToken)
        {
            try
            {
                var client = new WebClient().SetOauthToken(oauthToken);
                var zones = await GetZoneListAsync(client, projectId);

                var result = new List<GceInstance>();
                foreach (var zone in zones)
                {
                    var instances = await GetInstancesInZoneListAsync(client, projectId, zone.Name, oauthToken);
                    foreach (var instance in instances)
                    {
                        instance.ZoneName = zone.Name;
                        instance.ProjectId = projectId;
                        result.Add(instance);
                    }
                }
                return result;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download data: {ex.Message}");
            }
            return null;
        }

        public static async Task<IList<GceInstance>> GetInstancesInZoneListAsync(
            WebClient client,
            string projectId,
            string zoneName,
            string oauthToken)
        {
            var baseUrl = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones/{zoneName}/instances";
            return await ApiHelpers.LoadPagedListAsync<GceInstance, GceInstances>(
                client,
                baseUrl,
                x => x.Items,
                x => string.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}?pageToken={x.NextPageToken}");
        }

        /// <summary>
        /// Returns information about the given instance.
        /// </summary>
        /// <param name="projectId">The project id that contains the instance.</param>
        /// <param name="zoneName">The zone in which the instance lives.</param>
        /// <param name="name">The name of the instance,</param>
        /// <param name="oauthToken">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
        public static async Task<GceInstance> GetInstance(string projectId, string zoneName, string name, string oauthToken)
        {
            var url = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones/{zoneName}/instances/{name}";
            var client = new WebClient().SetOauthToken(oauthToken);
            var response = await client.DownloadStringTaskAsync(url);
            var result = JsonConvert.DeserializeObject<GceInstance>(response);
            result.ProjectId = projectId;
            result.ZoneName = zoneName;
            return result;
        }

        /// <summary>
        /// Given an instance already fetched, reload it's data and return a new instance with the fresh data.
        /// </summary>
        /// <param name="instance">The instance to refresh.</param>
        /// <param name="oauthToken">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
        public static Task<GceInstance> RefreshInstance(GceInstance instance, string oauthToken)
        {
            return GetInstance(projectId: instance.ProjectId, zoneName: instance.ZoneName, name: instance.Name, oauthToken: oauthToken);
        }

        /// <summary>
        /// Stores the given metadata in the target instance and returns the udpated instance after the change.
        /// </summary>
        /// <param name="src">The instance on which to store the data.</param>
        /// <param name="entries">The entries to store in the metadata of the instance.</param>
        /// <param name="oauthToken">The oauth token to use.</param>
        /// <returns></returns>
        public static async Task<GceInstance> StoreMetadata(GceInstance src, IList<MetadataEntry> entries, string oauthToken)
        {
            // Refresh the instance to get the latest metadata fingerprint.
            var target = await GceDataSource.RefreshInstance(src, oauthToken);

            var client = new WebClient().SetOauthToken(oauthToken);
            var url = $"https://www.googleapis.com/compute/v1/projects/{target.ProjectId}/zones/{target.ZoneName}/instances/{target.Name}/setMetadata";
            var request = new GceSetMetadataRequest
            {
                Fingerprint = target.Metadata.Fingerprint,
                Items = entries,
            };
            var serializedRequest = JsonConvert.SerializeObject(request);

            try
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var result = await client.UploadStringTaskAsync(url, "POST", serializedRequest);
                var operation = JsonConvert.DeserializeObject<ZoneOperation>(result);
                await operation.WaitForFinish(
                    project: target.ProjectId,
                    zone: target.ZoneName,
                    oauthToken: oauthToken);
            }
            catch (WebException ex)
            {
                var response = ex.Response;
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    var message = stream.ReadToEnd();
                    Debug.WriteLine($"Failed to update metadata: {message}");
                }
                throw;
            }

            // Returns the updated instance.
            return await RefreshInstance(target, oauthToken);
        }

        /// <summary>
        /// Returns the list of zones for the given project.
        /// </summary>
        /// <param name="client">The already authorized client to use to fetch data.</param>
        /// <param name="projectId">The project id for which to fetch the zone data.</param>
        /// <returns></returns>
        private static async Task<IList<Zone>> GetZoneListAsync(WebClient client, string projectId)
        {
            string baseUrl = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones";
            return await ApiHelpers.LoadPagedListAsync<Zone, Zones>(
                client,
                baseUrl,
                x => x.Items,
                x => string.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}?{x.NextPageToken}");
        }
    }
}
