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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PubSubWindows;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// A node that describes a Pubsub topic. A child of the Pubsub root and has Pubsub subscription childeren.
    /// </summary>
    internal class TopicViewModel : TopicViewModelBase
    {

        private const string IconResourcePath = "CloudExplorerSources/PubSub/Resources/topic_icon.png";

        private static readonly Lazy<ImageSource> s_topicIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        public TopicViewModel(PubsubSourceRootViewModel owner, Topic topic, IEnumerable<Subscription> subscriptions)
            : base(owner, new TopicItem(topic), subscriptions)
        {
            Icon = s_topicIcon.Value;
            ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerPubSubNewSubscriptionMenuHeader,
                        Command = new ProtectedCommand(OnNewSubscriptionCommand)
                    },
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerPubSubDeleteTopicMenuHeader,
                        Command = new ProtectedCommand(OnDeleteTopicCommand)
                    },
                    new MenuItem
                    {
                        Header = Resources.UiPropertiesMenuHeader,
                        Command = new ProtectedCommand(OnPropertiesWindowCommand)
                    }
                }
            };
        }

        /// <summary>
        /// Prompts the user for a new subscription, and creates it.
        /// </summary>
        private async void OnNewSubscriptionCommand()
        {
            IsLoading = true;
            try
            {

                try
                {
                    Subscription subscription = NewSubscriptionWindow.PromptUser(TopicItem.FullName);
                    if (subscription != null)
                    {
                        await DataSource.NewSubscriptionAsync(subscription);
                    }
                }
                catch (DataSourceException e)
                {
                    Debug.Write(e.Message, "New Subscription");
                    UserPromptUtils.ErrorPrompt(
                        Resources.PubSubNewSubscriptionErrorMessage, Resources.PubSubNewSubscriptionErrorHeader);
                }
                await Refresh();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Prompts the user about deleting the topic, and deletes it.
        /// </summary>
        private async void OnDeleteTopicCommand()
        {
            IsLoading = true;
            try
            {
                try
                {
                    bool doDelete = UserPromptUtils.ActionPrompt(
                        string.Format(Resources.PubSubDeleteTopicWindowMessage, TopicItem.Name),
                        Resources.PubSubDeleteTopicWindowHeader,
                        actionCaption: Resources.UiDeleteButtonCaption);
                    if (doDelete)
                    {
                        await DataSource.DeleteTopicAsync(TopicItem.Name);
                    }
                }
                catch (DataSourceException e)
                {
                    Debug.Write(e.Message, "Delete Topic");
                    UserPromptUtils.ErrorPrompt(
                        Resources.PubSubDeleteTopicErrorMessage, Resources.PubSubDeleteTopicErrorHeader);
                }
                Owner.Refresh();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Opens the properties window to this topic item.
        /// </summary>
        private void OnPropertiesWindowCommand()
        {
            Context.ShowPropertiesWindow(Item);
        }
    }
}
