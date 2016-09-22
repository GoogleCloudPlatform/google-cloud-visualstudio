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
using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.DataSources;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Data source that returns information about Pubsub resources.
    /// </summary>
    public class PubsubDataSource : DataSourceBase<PubsubService>
    {
        private string ProjectResourceName => $"projects/{ProjectId}";

        /// <summary>s
        /// Initializes a new data source that connects to Google Cloud Pub/Sub.
        /// </summary>
        /// <param name="projectId">The id of the project to connect to.</param>
        /// <param name="credential">The Google Credential to connect to.</param>
        /// <param name="applicationName">The name of the application. </param>
        public PubsubDataSource(string projectId, GoogleCredential credential, string applicationName) :
            base(projectId, credential, init => new PubsubService(init), applicationName)
        { }

        /// <summary>
        /// Gets all of the topics of the given project.
        /// </summary>
        public Task<IList<Topic>> GetTopicListAsync()
        {
            ProjectsResource.TopicsResource.ListRequest request =
                Service.Projects.Topics.List(ProjectResourceName);
            return LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                response => response.Topics,
                response => response.NextPageToken);
        }

        /// <summary>
        /// Gets all of the subscriptions of a given project.
        /// </summary>
        public Task<IList<Subscription>> GetSubscriptionListAsync()
        {
            ProjectsResource.SubscriptionsResource.ListRequest request =
                Service.Projects.Subscriptions.List(ProjectResourceName);
            return LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                response => response.Subscriptions,
                response => response.NextPageToken);
        }

        public Task<Topic> NewTopicAsync(string project, string topicName)
        {
            ProjectsResource.TopicsResource.CreateRequest request =
                Service.Projects.Topics.Create(new Topic(), $"projects/{project}/topics/{topicName}");
            return request.ExecuteAsync();
        }

        public async Task DeleteTopicAsync(string fullName)
        {
            ProjectsResource.TopicsResource.DeleteRequest request =
                Service.Projects.Topics.Delete(fullName);
            await request.ExecuteAsync();
        }

        public async Task<Subscription> NewSubscriptionAsync(
            string fullName, string topicFullName, int? ackDeadlineSeconds, bool push, string pushUrl)
        {
            var subscription = new Subscription
            {
                Name = fullName,
                Topic = topicFullName,
                AckDeadlineSeconds = ackDeadlineSeconds
            };
            if (push)
            {
                subscription.PushConfig = new PushConfig
                {
                    PushEndpoint = pushUrl
                };
            }
            ProjectsResource.SubscriptionsResource.CreateRequest request =
                Service.Projects.Subscriptions.Create(subscription, fullName);
            return await request.ExecuteAsync();
        }

        public async Task DeleteSubscriptionAsync(string fullName)
        {
            ProjectsResource.SubscriptionsResource.DeleteRequest request =
                Service.Projects.Subscriptions.Delete(fullName);
            await request.ExecuteAsync();
        }
    }
}
