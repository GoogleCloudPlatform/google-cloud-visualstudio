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
            var baseUrl = $"https://www.googleapis.com/storage/v1/b?project={projectId}";
            var client = new WebClient().SetOauthToken(oauthToken);

            return await ApiHelpers.LoadPagedListAsync<Bucket, BucketPage>(
                client,
                baseUrl,
                x => x.Items,
                x => string.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}&pageToken={x.NextPageToken}");
        }
    }
}