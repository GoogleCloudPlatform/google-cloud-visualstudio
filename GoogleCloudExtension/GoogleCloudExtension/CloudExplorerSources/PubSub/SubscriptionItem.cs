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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// An item that describes a Pubsub subscription.
    /// </summary>
    internal class SubscriptionItem : PropertyWindowItemBase
    {
        private readonly Subscription _subscription;

        public SubscriptionItem(Subscription subscription) :
            base(Resources.CloudExplorerPubSubSubscriptionCategory, subscription.Name)
        {
            _subscription = subscription;
        }

        /// <summary>
        /// The simple name of the subscription.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubSubscriptionCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubSubscriptionNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubSubscriptionNameDisplayName))]
        public string Name => PubsubDataSource.GetPathLeaf(_subscription.Name);

        /// <summary>
        /// The full name of the subscription: projects/projectId/subscriptions/subscriptionName.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubSubscriptionCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubSubscriptionFullNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubSubscriptionFullNameDisplayName))]
        public string FullName => _subscription.Name;

        /// <summary>
        /// The topic of the subscription.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubSubscriptionCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubSubscriptionTopicDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubSubscriptionTopicDisplayName))]
        public string Topic => _subscription.Topic;
    }
}
