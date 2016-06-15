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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.SQLAdmin.v1beta4;
using Google.Apis.SQLAdmin.v1beta4.Data;
using System.Diagnostics;
using Google;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about Google Cloud SQL instances and databases for a particular project and credentials.
    /// </summary>
    public class CloudSQLDataSource : DataSourceBase<SQLAdminService>
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="credential"></param>
        public CloudSQLDataSource(string projectId, GoogleCredential credential) : base(projectId, CreateService(credential))
        { }

        private static SQLAdminService CreateService(GoogleCredential credential)
        {
            return new SQLAdminService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
            });
        }

        /// <summary>
        /// Fetches the list of MySQL instances for the given project.
        /// </summary>
        /// <returns>The list of MySQL instances.</returns>
        public Task<IList<DatabaseInstance>> GetInstanceListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.Instances.List(ProjectId);
                    if (!String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"Fetching page: {token}");
                        request.PageToken = token;
                    }
                    else
                    {
                        Debug.WriteLine("Fetching first page.");
                    }
                    return request.ExecuteAsync();

                },
                x => x.Items,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Fetches the list of databases for a MySQL instance.
        /// </summary>
        /// <param name="instance"></param>
        public async Task<IList<Database>> GetDatabaseListAsync(string instance)
        {
            try
            {
                Debug.WriteLine($"Fetching databases for {instance}");
                var response = await Service.Databases.List(ProjectId, instance).ExecuteAsync();
                return response.Items;
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to call api: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
