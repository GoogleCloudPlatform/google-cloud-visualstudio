// Copyright 2017 Google Inc. All Rights Reserved.
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
using Google.Apis.CloudSourceRepositories.v1;
using Google.Apis.CloudSourceRepositories.v1.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source class that lists or creates Google Cloud Source Repositories.
    /// </summary>
    public class CsrDataSource : DataSourceBase<CloudSourceRepositoriesService>
    {
        /// <summary>
        /// Initializes an instance of <seealso cref="CsrDataSource"/> class.
        /// </summary>
        /// <param name="projectId">A Google Cloud Platform project id of the current user account.</param>
        /// <param name="credential">The credentials to use for the call.</param>
        /// <param name="appName">The name of the application.</param>
        public CsrDataSource(string projectId, GoogleCredential credential, string appName)
                : base(projectId, credential, init => new CloudSourceRepositoriesService(init), appName)
        { }

        /// <summary>
        /// Returns all repositories under the GCP project.
        /// </summary>
        public async Task<IList<Repo>> ListReposAsync()
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Projects.Repos.List(ProjectResourceName);
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.Repos,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Creates a cloud source repository.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        public async Task<Repo> CreateRepoAsync(string repoName)
        {
            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentNullException(nameof(repoName));
            }

            var body = new Repo { Name = $"{ProjectResourceName}/repos/{repoName}" };
            var request = Service.Projects.Repos.Create(body, ProjectResourceName);
            try
            {
                return await request.ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to create CSR repo: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
