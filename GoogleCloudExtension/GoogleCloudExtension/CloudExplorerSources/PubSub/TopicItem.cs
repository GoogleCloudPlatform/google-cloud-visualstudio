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
    /// An item that describes a topic.
    /// </summary>
    internal class TopicItem : PropertyWindowItemBase
    {
        private readonly Topic _topic;

        public TopicItem(Topic topic) :
            base(Resources.CloudExplorerPubSubTopicCategory, PubsubDataSource.GetPathLeaf(topic.Name))
        {
            _topic = topic;
        }

        /// <summary>
        /// The simple name of the topic.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubTopicCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubTopicNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubTopicNameDisplayName))]

        public string Name => PubsubDataSource.GetPathLeaf(_topic.Name);

        /// <summary>
        /// The full name, including project id, of the topic.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubTopicCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubTopicFullNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubTopicFullNameDisplayName))]
        public string FullName => _topic.Name;
    }
}
