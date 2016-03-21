// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns data about modules and versions in Google AppEngine. Implements the
    /// API calls according to https://cloud.google.com/appengine/docs/admin-api/
    /// </summary>
    public static class GaeDataSource
    {
        /// <summary>
        /// Returns a list of the services in the app associaged with <paramref name="projectId"/>.
        /// </summary>
        /// <param name="projectId">The project ID that contains the app.</param>
        /// <param name="oauthToken">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
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
                throw new DataSourceException(ex.Message);
            }
        }

        /// <summary>
        /// Returns the list of services for the service given by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service, in the form apps/{projetId}/services/{service}</param>
        /// <param name="oauthToken">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
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
