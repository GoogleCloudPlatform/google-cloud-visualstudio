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

using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Topic like item describing the container of orphaned subscriptions.
    /// </summary>
    internal class OrphanedSubscriptionsItem : ITopicItem
    {
        public const string DeletedTopicName = "_deleted-topic_";

        /// <summary>
        /// Display name of the orphaned subscriptions item.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubTopicCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubTopicNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubTopicNameDisplayName))]
        public string DisplayName => Resources.PubSubOrphanedSubscriptionsItemName;

        /// <summary>
        /// Name of the topic orphaned subscriptions claim to belong to.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubTopicCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubTopicFullNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubTopicFullNameDisplayName))]
        public string FullName => DeletedTopicName;
    }
}