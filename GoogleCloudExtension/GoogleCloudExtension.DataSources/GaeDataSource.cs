using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public static class GaeDataSource
    {
        public static async Task<IList<GaeService>> GetServicesAsync(string projectId, string oauthToken)
        {
            var baseUrl = $"https://appengine.googleapis.com/v1beta5/apps/{projectId}/services";
            var url = baseUrl;
            var client = new WebClient().SetOauthToken(oauthToken);
            try
            {
                var result = new List<GaeService>();
                while (true)
                {
                    Debug.WriteLine($"Requesting GAE Services: {url}");
                    var response = await client.DownloadStringTaskAsync(url);
                    var services = JsonConvert.DeserializeObject<GaeServices>(response);
                    result.AddRange(services.Services);
                    if (String.IsNullOrEmpty(services.NextPageToken))
                    {
                        break;
                    }
                    else
                    {
                        url = $"{baseUrl}?pageToken={services.NextPageToken}";
                    }
                }
                return result;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Request failed: {ex.Message}");
            }
            return null;
        }

        public static async Task<IList<GaeVersion>> GetServiceVersionsAsync(string name, string oauthToken)
        {
            var baseUrl = $"https://appengine.googleapis.com/v1beta5/{name}/versions";
            var url = $"{baseUrl}?view=FULL";
            var client = new WebClient().SetOauthToken(oauthToken);
            try
            {
                var result = new List<GaeVersion>();
                while (true)
                {
                    Debug.WriteLine($"Requesting versions: {url}");
                    var response = await client.DownloadStringTaskAsync(url);
                    var versions = JsonConvert.DeserializeObject<GaeVersions>(response);
                    result.AddRange(versions.Versions);
                    if (String.IsNullOrEmpty(versions.NextPageToken))
                    {
                        break;
                    }
                    else
                    {
                        url = $"{baseUrl}?view=FULL&pageToken={versions.NextPageToken}";
                    }
                }
                return result;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Request failed: {ex.Message}");
            }
            return null;
        }
    }
}
