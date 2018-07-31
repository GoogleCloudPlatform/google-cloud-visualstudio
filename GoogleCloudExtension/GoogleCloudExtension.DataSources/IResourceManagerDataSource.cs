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

using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// The interface of the ResourceManagerDataSource. Mock this object in unit tests.
    /// </summary>
    public interface IResourceManagerDataSource : IDataSourceBase<CloudResourceManagerService>
    {

        Task<IList<Project>> ProjectsListTask { get; }
        /// <summary>
        /// Returns the project given its <paramref name="projectId"/>.
        /// </summary>
        /// <param name="projectId">The project ID of the project to return.</param>
        Task<Project> GetProjectAsync(string projectId);

        /// <summary>
        /// Returns the complete list of projects for the current credentials.
        /// It always return empty list if no item is found, caller can safely assume there is no null return.
        /// </summary>
        Task<IList<Project>> GetProjectsListAsync();

        void RefreshProjects();
    }
}