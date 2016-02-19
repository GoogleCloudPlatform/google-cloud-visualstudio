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

        public static async Task StoreMetadata(GceInstance instance, string key, string value, string oauthToken)
        {
            var client = new WebClient();
            var url = $"https://www.googleapis.com/compute/v1/projects/{instance.ProjectId}/zones/{instance.ZoneName}/instances/{instance.Name}/setMetadata?access_token={oauthToken}";
            var request = new GceSetMetadataRequest
            {
                Fingerprint = instance.Metadata.Fingerprint,
                Items = new List<MetadataEntry>
                {
                    new MetadataEntry {Key = key, Value = value },
                }
            };
            var serializedRequest = JsonConvert.SerializeObject(request);
            var requestBytes = Encoding.ASCII.GetBytes(value);
            try
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var result = await client.UploadDataTaskAsync(url, "POST", requestBytes);
                var resultString = Encoding.ASCII.GetString(result);
                Debug.WriteLine($"Received output: {resultString}");
            }
            catch (WebException ex)
            {
                var response = ex.Response;
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    var message = stream.ReadToEnd();
                    Debug.WriteLine($"Failed to update metadata: {message}");
                }
            }
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
