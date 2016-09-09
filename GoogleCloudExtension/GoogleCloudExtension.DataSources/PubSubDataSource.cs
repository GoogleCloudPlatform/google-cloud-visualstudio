// // Copyright 2016 Google Inc. All Rights Reserved.
// //
// // Licensed under the Apache License, Version 2.0 (the "License");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

using Google.Apis.Auth.OAuth2;
using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.DataSources;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    public class PubSubDataSource : DataSourceBase<PubsubService>
    {

        public PubSubDataSource(string currentProjectId, GoogleCredential currentGoogleCredential,
            string applicationName) :
                base(currentProjectId, currentGoogleCredential, init => new PubsubService(init), applicationName)
        { }

        public async Task<IList<Topic>> GetTopicListAsync()
        {
            ProjectsResource.TopicsResource.ListRequest request = Service.Projects.Topics.List(ProjectId);
            return await LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                response => response.Topics,
                response => response.NextPageToken);
        }
    }
}
