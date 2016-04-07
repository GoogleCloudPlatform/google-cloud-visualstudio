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
    public static class GPlusDataSource
    {
        public static async Task<GPlusProfile> GetProfileAsync(string oauthToken)
        {
            var baseUrl = $"https://www.googleapis.com/plus/v1/people/me";
            try
            {
                var client = new WebClient().SetOauthToken(oauthToken);
                var response = await client.DownloadStringTaskAsync(baseUrl);
                return JsonConvert.DeserializeObject<GPlusProfile>(response);
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download data: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse result: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
