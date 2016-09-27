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

using GoogleCloudExtension.CloudExplorerSources.PubSub;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Data objet that backs the new subscription window. It contains the information needed to create a new
    /// subscription.
    /// </summary>
    public class NewSubscriptionData
    {

        public NewSubscriptionData(string topicFullName)
        {
            TopicFullName = topicFullName;
        }

        public string Name { get; set; }

        public string TopicName => PubsubSource.GetPathLeaf(TopicFullName);

        /// <summary>
        /// The full path name of the topic as given by the pubsub api.
        /// </summary>
        public string TopicFullName { get; }

        /// <summary>
        /// How long pub sub should wait for an acknoledgement before resending a message.
        /// </summary>
        public int? AckDeadlineSeconds { get; set; }

        /// <summary>
        /// If PubSub should send a push notification rather than waiting for a pull.
        /// </summary>
        public bool Push { get; set; } = false;

        /// <summary>
        /// The url to send a push notification too.
        /// </summary>
        public string PushUrl { get; set; }
    }
}
