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
using Google.Apis.SQLAdmin.v1beta4;
using Google.Apis.SQLAdmin.v1beta4.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about Google Cloud SQL instances and databases for a
    /// particular project and credentials.
    /// </summary>
    public class CloudSQLDataSource : DataSourceBase<SQLAdminService>
    {
        public const string RunnableState = "RUNNABLE";
        public const string SuspendedState = "SUSPENDED";
        public const string PendingCreateState = "PENDING_CREATE";
        public const string MaintenanceState = "MAINTENANCE";
        public const string UnknownState = "UNKNOWN_STATE";

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="credential"></param>
        public CloudSQLDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, CreateService(credential, appName), appName)
        { }

        private static SQLAdminService CreateService(GoogleCredential credential, string appName)
        {
            return new SQLAdminService(new Google.Apis.Services.BaseClientService.Initializer
            {
                ApplicationName = appName,
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
    }
}
