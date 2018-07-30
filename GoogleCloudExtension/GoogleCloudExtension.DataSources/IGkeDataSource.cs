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

using Google.Apis.Container.v1;
using Google.Apis.Container.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This interface defines the generic GkeDataSource, which allows dependency injection of sources.
    /// </summary>
    public interface IGkeDataSource : IDataSourceBase<ContainerService>
    {
        /// <summary>
        /// Lists all of the clusters in the current project.
        /// </summary>
        /// <returns>The list of clusters.</returns>
        Task<IList<Cluster>> GetClusterListAsync();
    }
}
