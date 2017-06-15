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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// Helper methods common to Cloud Source Repositories feature
    /// </summary>
    public static class CsrUtils
    {
        /// <summary>
        /// Gets repository name 
        /// The Cloud Repository name has format $"{ProjectResourceName}/repos/{repoName}" };
        /// Returns the last part which is the repo name.
        /// </summary>
        /// <param name="cloudRepo">Repository object</param>
        /// <returns>Repository name</returns>
        public static string GetRepoName(Repo cloudRepo) => cloudRepo?.Name?.Split('/').LastOrDefault();

        /// <summary>
        /// Parse the repository url and get the project name portion
        /// </summary>
        /// <param name="url">
        /// The remote repository URL.
        /// The format:
        /// https://source.developers.google.com/p/{project-id}/r/{repository-name}
        /// </param>
        /// <returns>A project id or NULL if the format is not as expected.</returns>
        public static string ParseProjectId(string url)
        {
            url.ThrowIfNullOrEmpty(nameof(url));
            if (url.StartsWith("https://source.developers.google.com", StringComparison.OrdinalIgnoreCase))
            {
                string[] splits = url.Split('/');
                return splits?.Length >= 3 ? splits[splits.Length - 3] : null;
            }
            return null;
        }

        /// <summary>
        /// Retrives the list of projects that belongs to current account.
        /// </summary>
        public static async Task<List<Project>> GetProjectsAsync()
        {
            var results = await CreateResourceManagerDataSource()?.GetProjectsListAsync();
            return results?.Where(x => x.LifecycleState == "ACTIVE").ToList();
        }

        /// <summary>
        /// Retrives the list of <seealso cref="Repo"/> under the project.
        /// </summary>
        public static async Task<IList<Repo>> GetCloudReposAsync(Project project)
        {
            var csrDataSource = CreateCsrDataSource(project.ProjectId);
            try
            {
                return await csrDataSource?.ListReposAsync();
            }
            catch (DataSourceException ex)
            {
                // Call out "no permission" project.
                var innerEx = ex.InnerException as Google.GoogleApiException;
                if (innerEx.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Debug.WriteLine($"No permission to query repos from project {project.Name}");
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// Create <seealso cref="CsrDataSource"/> object for the project.
        /// </summary>
        public static CsrDataSource CreateCsrDataSource(string projectId)
        {
            if (String.IsNullOrWhiteSpace(projectId) || CredentialsStore.Default.CurrentGoogleCredential == null)
            {
                return null;
            }
            return new CsrDataSource(
                projectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
        }

        private static ResourceManagerDataSource CreateResourceManagerDataSource()
        {
            if (CredentialsStore.Default.CurrentGoogleCredential == null)
            {
                return null;
            }
            return new ResourceManagerDataSource(
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
        }
    }
}
