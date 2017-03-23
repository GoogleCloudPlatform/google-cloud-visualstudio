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

using Google;
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
        /// Gets all of the topics of the current project.
        /// </summary>
        public async Task<IList<Topic>> GetTopicListAsync()
        {
            try
            {
                ProjectsResource.TopicsResource.ListRequest request =
                    Service.Projects.Topics.List(ProjectResourceName);
                return await LoadPagedListAsync(
                    token =>
                    {
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    },
                    response => response.Topics,
                    response => response.NextPageToken);
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException("Error listing topics", e);
            }
        }

        /// <summary>
        /// Gets all of the subscriptions of a current project.
        /// </summary>
        public async Task<IList<Subscription>> GetSubscriptionListAsync()
        {
            try
            {
                ProjectsResource.SubscriptionsResource.ListRequest request =
                    Service.Projects.Subscriptions.List(ProjectResourceName);
                return await LoadPagedListAsync(
                    token =>
                    {
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    },
                    response => response.Subscriptions,
                    response => response.NextPageToken);
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException("Error listing subscriptions", e);
            }
        }

        /// <summary>
        /// Creates a new topic.
        /// </summary>
        /// <param name="topicName">The name of the topic. Does not include the project id.</param>
        public async Task<Topic> NewTopicAsync(string topicName)
        {
            try
            {
                ProjectsResource.TopicsResource.CreateRequest request =
                    Service.Projects.Topics.Create(new Topic(), GetTopicFullName(topicName));
                return await request.ExecuteAsync();
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException("Error creating new topic", e);
            }
        }

        /// <summary>
        /// Deletes a topic.
        /// </summary>
        /// <param name="topicName">The name of the topic. Does not include the project id.</param>
        public async Task DeleteTopicAsync(string topicName)
        {
            try
            {
                ProjectsResource.TopicsResource.DeleteRequest request =
                    Service.Projects.Topics.Delete(GetTopicFullName(topicName));
                await request.ExecuteAsync();
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException("Error deleting topic", e);
            }
        }

        /// <summary>
        /// Creates a new subscription.
        /// </summary>
        public async Task<Subscription> NewSubscriptionAsync(Subscription subscription)
        {
            try
            {
                string subscriptionFullName = GetSubscriptionFullName(subscription.Name);
                ProjectsResource.SubscriptionsResource.CreateRequest request =
                    Service.Projects.Subscriptions.Create(subscription, subscriptionFullName);
                return await request.ExecuteAsync();
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException(e.Error?.Message ?? "", e);
            }
        }

        /// <summary>
        /// Deletes a subscription
        /// </summary>
        /// <param name="subscriptionName">The name of the subscription to delete. Does not include the project id.</param>
        public async Task DeleteSubscriptionAsync(string subscriptionName)
        {
            try
            {
                ProjectsResource.SubscriptionsResource.DeleteRequest request =
                    Service.Projects.Subscriptions.Delete(GetSubscriptionFullName(subscriptionName));
                await request.ExecuteAsync();
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException("Error deleting subscription", e);
            }
        }

        private string GetTopicFullName(string topicName)
        {
            return $"projects/{ProjectId}/topics/{topicName}";
        }

        private string GetSubscriptionFullName(string subscriptionName)
        {
            return $"projects/{ProjectId}/subscriptions/{subscriptionName}";
        }
    }
}
