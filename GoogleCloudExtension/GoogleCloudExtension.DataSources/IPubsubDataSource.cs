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

using Google.Apis.Pubsub.v1.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Inteface of the PubsubDataSource.
    /// </summary>
    public interface IPubsubDataSource
    {
        /// <summary>
        /// Gets all of the topics of the current project.
        /// </summary>
        Task<IList<Topic>> GetTopicListAsync();

        /// <summary>
        /// Gets all of the subscriptions of a current project.
        /// </summary>
        Task<IList<Subscription>> GetSubscriptionListAsync();

        /// <summary>
        /// Creates a new topic.
        /// </summary>
        /// <param name="topicName">The name of the topic. Does not include the project id.</param>
        Task<Topic> NewTopicAsync(string topicName);

        /// <summary>
        /// Deletes a topic.
        /// </summary>
        /// <param name="topicFullName">The full name of the topic.</param>
        Task DeleteTopicAsync(string topicFullName);

        /// <summary>
        /// Creates a new subscription.
        /// </summary>
        Task<Subscription> NewSubscriptionAsync(Subscription subscription);

        /// <summary>
        /// Deletes a subscription
        /// </summary>
        /// <param name="subscriptionFullName">The full name of the subscription to delete.</param>
        Task DeleteSubscriptionAsync(string subscriptionFullName);
    }
}