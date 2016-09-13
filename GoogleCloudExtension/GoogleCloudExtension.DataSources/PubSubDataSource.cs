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
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    public class PubSubDataSource : DataSourceBase<PubsubService>
    {
        /// <summary>
        /// Initializes a new data source that connects to Google Cloud Pub/Sub.
        /// </summary>
        /// <param name="projectId">The id of the project to connect to.</param>
        /// <param name="credential">The Google Credential to connect to.</param>
        /// <param name="applicationName">The name of the application. </param>
        public PubSubDataSource(string projectId, GoogleCredential credential, string applicationName) :
            base(projectId, credential, init => new PubsubService(init), applicationName)
        { }

        public Task<IList<Topic>> GetTopicListAsync()
        {
            ProjectsResource.TopicsResource.ListRequest request =
                Service.Projects.Topics.List($"projects/{ProjectId}");
            return LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                response => response.Topics,
                response => response.NextPageToken);
        }

        public Task<IList<Subscription>> GetSubscriptionListAsync()
        {
            ProjectsResource.SubscriptionsResource.ListRequest request =
                Service.Projects.Subscriptions.List($"projects/{ProjectId}");
            return LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                response => response.Subscriptions,
                response => response.NextPageToken);
        }

        public Task<IList<string>> GetTopicSubscriptionListAsync(string topicName)
        {
            var request = Service.Projects.Topics.Subscriptions.List(topicName);
            return LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                response => response.Subscriptions,
                response => response.NextPageToken);
        }

        public async Task<IList<ReceivedMessage>> PullSubscriptionMessagesAsync(Subscription subscription)
        {
            ProjectsResource.SubscriptionsResource.PullRequest request =
                Service.Projects.Subscriptions.Pull(new PullRequest(), subscription.Name);
            PullResponse response = await request.ExecuteAsync();
            return response.ReceivedMessages;
        }

        public Task<Empty> RemoveMessagesAsync(
            Subscription subscription, IEnumerable<ReceivedMessage> messages)
        {
            var body = new AcknowledgeRequest
            {
                AckIds = messages.Select(message => message.AckId).ToList()
            };
            ProjectsResource.SubscriptionsResource.AcknowledgeRequest request =
                Service.Projects.Subscriptions.Acknowledge(body, subscription.Name);
            return request.ExecuteAsync();
        }
    }
}
