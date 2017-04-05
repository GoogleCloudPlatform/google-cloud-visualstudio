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
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PubSubWindows;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// A node that describes a Pubsub topic. A child of the Pubsub root and has Pubsub subscription childeren.
    /// </summary>
    internal class TopicViewModel : TreeHierarchy, ICloudExplorerItemSource
    {

        private const string IconResourcePath = "CloudExplorerSources/PubSub/Resources/topic_icon.png";

        private static readonly Lazy<ImageSource> s_topicIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private static TreeLeaf ErrorPlaceholder { get; } = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubListSubscriptionsErrorCaption,
            IsError = true
        };
        private static TreeLeaf LoadingPlaceholder { get; } = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubLoadingSubscriptionsCaption,
            IsLoading = true
        };

        private PubsubSourceRootViewModel _owner;
        private TopicItem _topicItem;
        private bool _isRefreshing;

        /// <summary>
        /// The PubsubDataSource to connect to.
        /// </summary>
        public PubsubDataSource DataSource => _owner.DataSource;

        /// <summary>
        /// The topic item of this view model.
        /// </summary>
        public object Item => _topicItem;

        /// <summary>
        /// Returns the context in which this view model is working.
        /// </summary>
        public ICloudSourceContext Context => _owner.Context;

        public event EventHandler ItemChanged;

        public TopicViewModel(
            PubsubSourceRootViewModel owner, Topic topic, IEnumerable<Subscription> subscriptions)
        {
            _owner = owner;
            _topicItem = new TopicItem(topic);
            Caption = _topicItem.Name;
            Icon = s_topicIcon.Value;
            AddSubscriptonsOfTopic(subscriptions);
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
        /// Refreshes the subscriptions of this topic.
        /// </summary>
        public async Task Refresh()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                Children.Clear();
                Children.Add(LoadingPlaceholder);
                IList<Subscription> subscriptions = await DataSource.GetSubscriptionListAsync();
                Children.Clear();
                if (subscriptions != null)
                {
                    AddSubscriptonsOfTopic(subscriptions);
                }
            }
            catch (Exception e)
            {
                Debug.Write(e, "Refresh Subscriptions");
                Children.Add(ErrorPlaceholder);
            }
            finally
            {
                _isRefreshing = false;
            }
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
                    Subscription subscription = NewSubscriptionWindow.PromptUser(_topicItem.FullName);
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
                        string.Format(Resources.PubSubDeleteTopicWindowMessage, _topicItem.Name),
                        Resources.PubSubDeleteTopicWindowHeader,
                        actionCaption: Resources.UiDeleteButtonCaption);
                    if (doDelete)
                    {
                        await DataSource.DeleteTopicAsync(_topicItem.Name);
                    }
                }
                catch (DataSourceException e)
                {
                    Debug.Write(e.Message, "Delete Topic");
                    UserPromptUtils.ErrorPrompt(
                        Resources.PubSubDeleteTopicErrorMessage, Resources.PubSubDeleteTopicErrorHeader);
                }
                _owner.Refresh();
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

        /// <summary>
        /// Adds the child subscriptions from the enumeration of all subscriptions.
        /// </summary>
        /// <param name="subscriptions">An enumeration of all subscriptions.</param>
        private void AddSubscriptonsOfTopic(IEnumerable<Subscription> subscriptions)
        {
            Func<Subscription, bool> isTopicMemeber = subscription => subscription.Topic == _topicItem.FullName;
            foreach (Subscription subscription in subscriptions.Where(isTopicMemeber))
            {
                Children.Add(new SubscriptionViewModel(this, subscription));
            }
        }
    }
}
