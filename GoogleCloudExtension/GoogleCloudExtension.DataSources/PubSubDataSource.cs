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
using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Data source that returns information about Pubsub resources.
    /// </summary>
    public class PubsubDataSource : DataSourceBase<PubsubService>, IPubsubDataSource
    {
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
        /// For Testing.
        /// </summary>
        internal PubsubDataSource(PubsubService service, string projectId) : base(projectId, null, init => service, null) { }

        /// <summary>
        /// Gets all of the topics of the current project.
        /// </summary>
        public async Task<IList<Topic>> GetTopicListAsync()
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

        /// <summary>
        /// Gets all of the subscriptions of a current project.
        /// </summary>
        public async Task<IList<Subscription>> GetSubscriptionListAsync()
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
                throw new DataSourceException(e.Message, e);
            }
        }

        /// <summary>
        /// Deletes a topic.
        /// </summary>
        /// <param name="topicFullName">The full name of the topic.</param>
        public async Task DeleteTopicAsync(string topicFullName)
        {
            try
            {
                ProjectsResource.TopicsResource.DeleteRequest request =
                    Service.Projects.Topics.Delete(topicFullName);
                await request.ExecuteAsync();
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException(e.Message, e);
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
                throw new DataSourceException(e.Message, e);
            }
        }

        /// <summary>
        /// Deletes a subscription
        /// </summary>
        /// <param name="subscriptionFullName">The full name of the subscription to delete.</param>
        public async Task DeleteSubscriptionAsync(string subscriptionFullName)
        {
            try
            {
                ProjectsResource.SubscriptionsResource.DeleteRequest request =
                    Service.Projects.Subscriptions.Delete(subscriptionFullName);
                await request.ExecuteAsync();
            }
            catch (GoogleApiException e)
            {
                throw new DataSourceException(e.Message, e);
            }
        }

        /// <summary>
        /// Gets the last part of the full name i.e. the leaf of the path.
        /// </summary>
        /// <param name="fullTopicName">The full topic name (e.g. <code>"projects/project-id/topics/topic-name"</code>)</param>
        public static string GetPathLeaf(string fullTopicName)
        {
            return GetPathSections(fullTopicName).LastOrDefault();
        }

        /// <summary>
        /// Gets the project part of a full topic name.
        /// </summary>
        /// <param name="fullTopicName">The full topic name (e.g. <code>"projects/project-id/topics/topic-name"</code>)</param>
        /// <returns>The project id part of the full topic name. This must be the second section.</returns>
        public static string GetTopicProject(string fullTopicName)
        {
            return GetPathSections(fullTopicName).Skip(1).FirstOrDefault();
        }

        private static string[] GetPathSections(string path)
        {
            return path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Gets the full name including project id from a simple topic name.
        /// </summary>
        /// <param name="topicName">The simple topic name.</param>
        /// <returns>The full topic name.</returns>
        private string GetTopicFullName(string topicName) => $"projects/{ProjectId}/topics/{topicName}";

        /// <summary>
        /// Gets the full name including project id from a simple subscription name.
        /// </summary>
        /// <param name="subscriptionName">The simple subscription name.</param>
        /// <returns>The full subscription name.</returns>
        private string GetSubscriptionFullName(string subscriptionName) =>
            $"projects/{ProjectId}/subscriptions/{subscriptionName}";
    }
}
