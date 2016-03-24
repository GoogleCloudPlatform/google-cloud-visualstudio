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
            var client = new WebClient().SetOauthToken(oauthToken);

            return await ApiHelpers.LoadPagedListAsync<GaeService, GaeServices>(
                client,
                baseUrl,
                x => x.Services,
                x => String.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}?pageToken={x.NextPageToken}");
        }

        /// <summary>
        /// Returns the list of services for the service given by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the service, in the form apps/{projetId}/services/{service}</param>
        /// <param name="oauthToken">The oauth token to use to authenticate the call.</param>
        /// <returns></returns>
        public static async Task<IList<GaeVersion>> GetServiceVersionsAsync(string name, string oauthToken)
        {
            var baseUrl = $"https://appengine.googleapis.com/v1beta5/{name}/versions?view=FULL";
            var client = new WebClient().SetOauthToken(oauthToken);

            return await ApiHelpers.LoadPagedListAsync<GaeVersion, GaeVersions>(
                client,
                baseUrl,
                x => x.Versions,
                x => String.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}&pageToken={x.NextPageToken}");
        }
    }
}
