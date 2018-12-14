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
using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Services;
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
        /// LifecycleState constants.
        /// <see href="https://cloud.google.com/resource-manager/reference/rest/v1/projects#LifecycleState"/>
        /// </summary>
        public static class LifecycleState
        {
            public const string Active = "ACTIVE";
            public const string DeleteRequested = "DELETE_REQUESTED";
        }

        /// <summary>
        /// The task to get the list of projects associated with the account this data source was configured for.
        /// </summary>
        public Task<IList<Project>> ProjectsListTask { get; private set; }

        /// <param name="credential">The credentials to use for the service.</param>
        /// <param name="appName">The name of the application.</param>
        public ResourceManagerDataSource(GoogleCredential credential, string appName)
            : this(credential, init => new CloudResourceManagerService(init), appName)
        { }

        /// <summary>
        /// For unit testing.
        /// </summary>
        /// <param name="credential">The credentials to use for the service.</param>
        /// <param name="factory">The service factory function.</param>
        /// <param name="appName">The name of the application.</param>
        internal ResourceManagerDataSource(
            GoogleCredential credential,
            Func<BaseClientService.Initializer, CloudResourceManagerService> factory,
            string appName) : base(credential, factory, appName)
        {
            ProjectsListTask = GetProjectsListAsync();
        }

        /// <summary>
        /// Returns the complete list of projects for the current credentials.
        /// It always return empty list if no item is found, caller can safely assume there is no null return.
        /// </summary>
        public Task<IList<Project>> GetProjectsListAsync()
        {
            return LoadPagedListAsync(
                token =>
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine("Fetching first page.");
                        return Service.Projects.List().ExecuteAsync();
                    }
                    else
                    {
                        Debug.WriteLine($"Fetching page: {token}");
                        ProjectsResource.ListRequest request = Service.Projects.List();
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Projects,
                x => x.NextPageToken,
                predicate: x => x.LifecycleState == LifecycleState.Active);
        }

        /// <summary>
        /// Refreshes ProjectsListTask so it includes any newly created projects.
        /// </summary>
        public void RefreshProjects() => ProjectsListTask = GetProjectsListAsync();

        /// <summary>
        /// Returns the project given its <paramref name="projectId"/>.
        /// </summary>
        /// <param name="projectId">The project ID of the project to return.</param>
        public async Task<Project> GetProjectAsync(string projectId)
        {
            try
            {
                return await Service.Projects.Get(projectId).ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                if (ex.Error?.Code == 404)
                {
                    Debug.WriteLine($"Could not find project: {projectId}");
                    return null;
                }

                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
