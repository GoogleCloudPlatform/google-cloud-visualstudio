// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google;
using Google.Apis.Appengine.v1;
using Google.Apis.Appengine.v1.Data;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about GAE apps and keeps track of operations in flight.
    /// </summary>
    public class GaeDataSource : DataSourceBase<AppengineService>
    {

        /// <summary>
        /// Initializes an instance of the data source.
        /// </summary>
        /// <param name="projectId">The project id that contains the GAE instances to manipulate.</param>
        /// <param name="credential">The credentials to use for the call.</param>
        public GaeDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new AppengineService(init), appName)
        { }

        /// <summary>
        /// Fetches the list of GAE services for the given project.
        /// </summary>
        /// <returns>The list of GAE services.</returns>
        public async Task<IList<Service>> GetServiceListAsync()
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Apps.Services.List(ProjectId);
                    if (!String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetServiceListAsync)}: Fetching page: {token}");
                        request.PageToken = token;
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetServiceListAsync)}: Fetching first page.");
                    }
                    return request.ExecuteAsync();
                },
                x => x.Services,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Fetches the list of GAE versions for the given project and service.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <returns>The list of GAE versions.</returns>
        public async Task<IList<Google.Apis.Appengine.v1.Data.Version>> GetVersionListAsync(string serviceId)
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Apps.Services.Versions.List(ProjectId, serviceId);
                    if (!String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetVersionListAsync)}: Fetching page: {token}");
                        request.PageToken = token;
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetVersionListAsync)}: Fetching first page.");
                    }
                    return request.ExecuteAsync();
                },
                x => x.Versions,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Fetches the list of GAE instance for the given project, service and version.
        /// </summary>
        /// <param name="serviceId">The id of the service</param>
        /// <param name="versionId">The id of the version</param>
        /// <returns>The list of GAE versions.</returns>
        public async Task<IList<Instance>> GetInstanceListAsync(string serviceId, string versionId)
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Apps.Services.Versions.Instances.List(ProjectId, serviceId, versionId);
                    if (!String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetInstanceListAsync)}: Fetching page: {token}");
                        request.PageToken = token;
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GaeDataSource)}, {nameof(GetInstanceListAsync)}: Fetching first page.");
                    }
                    return request.ExecuteAsync();
                },
                x => x.Instances,
                x => x.NextPageToken);
        }
    }
}
