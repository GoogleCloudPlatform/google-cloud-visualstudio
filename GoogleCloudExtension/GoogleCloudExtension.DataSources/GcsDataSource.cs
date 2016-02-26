// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public static class GcsDataSource
    {
        public static async Task<IList<Bucket>> GetBucketListAsync(string projectId, string oauthToken)
        {
            try
            {
                var client = new WebClient().SetOauthToken(oauthToken);
                var url = $"https://www.googleapis.com/storage/v1/b?project={projectId}";
                var content = await client.DownloadStringTaskAsync(url);

                var buckets = JsonConvert.DeserializeObject<Buckets>(content);
                return buckets.Items;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to download data: {ex.Message}");
            }
            return null;
        }
    }
}