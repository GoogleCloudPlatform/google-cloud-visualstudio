// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about Google Cloud Storage buckets. Calls the API according
    /// to the documentation at https://cloud.google.com/storage/docs/json_api/.
    /// </summary>
    public static class GcsDataSource
    {
        /// <summary>
        /// Fetches the list of buckets for the given project.
        /// </summary>
        /// <param name="projectId">The id of the project that owns the buckets.</param>
        /// <param name="oauthToken">The oauth token to use to authorize the call.</param>
        /// <returns>The list of buckets.</returns>
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