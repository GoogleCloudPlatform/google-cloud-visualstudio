// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                // 1) Request the list of zones for this project.
                var zones = await GetZoneListAsync(projectId, oauthToken);

                //  2) Request in parallel the instances in each zone.
                var result = new List<GceInstance>();
                var requestResults = zones
                    .Select(x => GetInstancesInZoneListAsync(projectId, x.Name, oauthToken));

                // 3) Merge the results into a single list.
                foreach (var instancesPerZone in await Task.WhenAll(requestResults))
                {
                    result.AddRange(instancesPerZone);
                }
                return result;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download data: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse response: {ex.Message}");
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
        public static async Task<GceInstance> GetInstance(string projectId, string zoneName, string name, string oauthToken)
        {
            try
            {
                var url = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones/{zoneName}/instances/{name}";
                var client = new WebClient().SetOauthToken(oauthToken);
                var response = await client.DownloadStringTaskAsync(url);
                var result = JsonConvert.DeserializeObject<GceInstance>(response);
                result.ProjectId = projectId;
                result.ZoneName = zoneName;
                return result;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download data: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse response: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
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

            try
            {
                var request = new GceSetMetadataRequest
                {
                    Fingerprint = target.Metadata.Fingerprint,
                    Items = entries,
                };
                var serializedRequest = JsonConvert.SerializeObject(request);
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var result = await client.UploadStringTaskAsync(url, "POST", serializedRequest);
                var operation = JsonConvert.DeserializeObject<ZoneOperation>(result);
                await operation.Wait(
                    project: target.ProjectId,
                    zone: target.ZoneName,
                    oauthToken: oauthToken);
                // Returns the updated instance.
                return await RefreshInstance(target, oauthToken);
            }
            catch (WebException ex)
            {
                var response = ex.Response;
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    var message = stream.ReadToEnd();
                    Debug.WriteLine($"Failed to update metadata: {message}");
                }
                throw new DataSourceException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse response: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Fetches the list of zones for the given project.
        /// </summary>
        /// <param name="projectId">The project id for which to fetch the zone data.</param>
        /// <param name="oauthToken">The auth token to use to authenticate this call.</param>
        /// <returns></returns>
        private static async Task<IList<Zone>> GetZoneListAsync(string projectId, string oauthToken)
        {
            var client = new WebClient().SetOauthToken(oauthToken);

            string baseUrl = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones";
            return await ApiHelpers.LoadPagedListAsync<Zone, ZonePage>(
                client,
                baseUrl,
                x => x.Items,
                x => string.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}?{x.NextPageToken}");
        }

        /// <summary>
        /// Fetches the list of instances in the given zone and project.
        /// </summary>
        /// <param name="projectId">The project that contains the instances to fetch.</param>
        /// <param name="zoneName">The zone name where the instance lies.</param>
        /// <param name="oauthToken">The auth token to use to authenticate this call.</param>
        /// <returns></returns>
        private static async Task<IList<GceInstance>> GetInstancesInZoneListAsync(
            string projectId,
            string zoneName,
            string oauthToken)
        {
            var baseUrl = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones/{zoneName}/instances";
            var client = new WebClient().SetOauthToken(oauthToken);
            var result = await ApiHelpers.LoadPagedListAsync<GceInstance, GceInstancePage>(
                client,
                baseUrl,
                x => x.Items,
                x => string.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}?pageToken={x.NextPageToken}");
            foreach (var instance in result)
            {
                instance.ZoneName = zoneName;
                instance.ProjectId = projectId;
            }
            return result;
        }

    }
}
