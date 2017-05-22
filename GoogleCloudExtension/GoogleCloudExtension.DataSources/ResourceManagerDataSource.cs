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

using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class wraps the <seealso cref="Google.Apis.CloudResourceManager.v1.CloudResourceManagerService"/> class for
    /// a given set of credentials.
    /// </summary>
    public class ResourceManagerDataSource : DataSourceBase<CloudResourceManagerService>, IResourceManagerDataSource
    {
        /// <summary>
        /// The constructor for the class.
        /// </summary>
        /// <param name="credential"></param>
        public ResourceManagerDataSource(GoogleCredential credential, string appName)
            : base(credential, init => new CloudResourceManagerService(init), appName)
        { }

        /// <summary>
        /// Returns the complete list of projects for the current credentials.
        /// </summary>
        public Task<IList<Project>> GetProjectsListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine("Fetching first page.");
                        return Service.Projects.List().ExecuteAsync();
                    }
                    else
                    {
                        Debug.WriteLine($"Fetching page: {token}");
                        var request = Service.Projects.List();
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Projects,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Returns the project given its <paramref name="projectId"/>.
        /// </summary>
        /// <param name="projectId">The project ID of the project to return.</param>
        public Task<Project> GetProjectAsync(string projectId)
        {
            return Service.Projects.Get(projectId).ExecuteAsync();
        }
    }
}
