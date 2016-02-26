// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public class GceDataSource
    {
        public static async Task<IList<GceInstance>> GetInstanceListAsync(string projectId, string oauthToken)
        {
            try
            {
                var client = new WebClient();
                var zones = await GetZoneListAsync(client, projectId, oauthToken);

                var result = new List<GceInstance>();
                foreach (var zone in zones)
                {
                    var url = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones/{zone.Name}/instances?access_token={oauthToken}";
                    var content = await client.DownloadStringTaskAsync(url);
                    var instances = JsonConvert.DeserializeObject<GceInstances>(content);
                    if (instances.Items != null)
                    {
                        foreach (var instance in instances.Items)
                        {
                            instance.ZoneName = zone.Name;
                            instance.ProjectId = projectId;
                            result.Add(instance);
                        }
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

        public static async Task<GceInstance> GetInstance(string projectId, string zoneName, string name, string oauthToken)
        {
            var url = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones/{zoneName}/instances/{name}?access_token={oauthToken}";
            var client = new WebClient();
            var response = await client.DownloadStringTaskAsync(url);
            var result = JsonConvert.DeserializeObject<GceInstance>(response);
            result.ProjectId = projectId;
            result.ZoneName = zoneName;
            return result;
        }

        public static Task<GceInstance> RefreshInstance(GceInstance instance, string oauthToken)
        {
            return GetInstance(projectId: instance.ProjectId, zoneName: instance.ZoneName, name: instance.Name, oauthToken: oauthToken);
        }

        /// <summary>
        /// Stores the given metadata in the target instance and returns the udpated instance after the change.
        /// </summary>
        /// <param name="src">The instance on which to store the data.</param>
        /// <param name="key">The key on which to store data.</param>
        /// <param name="value">The data to store.</param>
        /// <param name="oauthToken">The oauth token to use.</param>
        /// <returns></returns>
        public static async Task<GceInstance> StoreMetadata(GceInstance src, string key, string value, string oauthToken)
        {
            // Refresh the instance to get the latest metadata fingerprint.
            var target = await GceDataSource.RefreshInstance(src, oauthToken);

            var client = new WebClient();
            var url = $"https://www.googleapis.com/compute/v1/projects/{target.ProjectId}/zones/{target.ZoneName}/instances/{target.Name}/setMetadata?access_token={oauthToken}";
            var request = new GceSetMetadataRequest
            {
                Fingerprint = target.Metadata.Fingerprint,
                Items = new List<MetadataEntry>
                {
                    new MetadataEntry {Key = key, Value = value },
                }
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

        private static async Task<IList<Zone>> GetZoneListAsync(WebClient client, string projectId, string accessToken)
        {
            try
            {
                var url = $"https://www.googleapis.com/compute/v1/projects/{projectId}/zones?access_token={accessToken}";
                var content = await client.DownloadStringTaskAsync(url);

                var zones = JsonConvert.DeserializeObject<Zones>(content);
                return zones.Items;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download list of zone: {ex.Message}");
            }
            return null;
        }
    }
}
