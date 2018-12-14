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
using Google.Apis.Container.v1;
using Google.Apis.Container.v1.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that fetches information from Google Container Engine.
    /// </summary>
    public class GkeDataSource : DataSourceBase<ContainerService>, IGkeDataSource
    {
        // This value means to fetch data from all the zones and regions.
        private const string AllLocationsValue = "-";

        public GkeDataSource(string projectId, GoogleCredential credential, string appName) :
            base(projectId, credential, init => new ContainerService(init), appName)
        { }

        // For testing.
        internal GkeDataSource(ContainerService mockedService, string projectId)
            : base(projectId, null, init => mockedService, null) { }

        /// <summary>
        /// Lists all of the clusters in the current project.
        /// </summary>
        /// <returns>The list of clusters.</returns>
        public async Task<IList<Cluster>> GetClusterListAsync()
        {
            try
            {
                ListClustersResponse clustersResponse = await Service.Projects.Locations.Clusters
                    .List($"projects/{ProjectId}/locations/{AllLocationsValue}")
                    .ExecuteAsync();
                return clustersResponse.Clusters;
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to list clusters: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
