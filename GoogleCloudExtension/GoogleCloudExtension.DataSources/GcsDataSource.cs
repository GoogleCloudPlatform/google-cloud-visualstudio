// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.DataSources.Models;
using System;
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
        public static Task<IList<Bucket>> GetBucketListAsync(string projectId, GoogleCredential credential)
        {
            var service = new StorageService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
            });

            try
            {
                return ApiHelpers.NewLoadPagedListAsync<Bucket, Buckets>(
                    (token) =>
                    {
                        if (token != null)
                        {
                            Debug.WriteLine($"Loading page: {token}");
                            var request = service.Buckets.List(projectId);
                            request.PageToken = token;
                            return request.ExecuteAsync();
                        }
                        else
                        {
                            Debug.WriteLine("Loading final page.");
                            return service.Buckets.List(projectId).ExecuteAsync();
                        }
                    },
                    x => x.Items,
                    x => x.NextPageToken);
            }
            catch (Exception ex)
            {
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}