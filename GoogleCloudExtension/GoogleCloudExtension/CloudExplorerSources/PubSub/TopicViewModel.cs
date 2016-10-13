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
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PubSubWindows;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// A node that describes a Pubsub topic. A child of the Pubsub root and has Pubsub subscription childeren.
    /// </summary>
    internal class TopicViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        // TODO(Jimwp): Use new pubsub specific icon.
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/bucket_icon.png";
        private static readonly Lazy<ImageSource> s_topicIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));
        private PubsubSourceRootViewModel _owner;

        private TopicItem _topicItem;
        public PubsubDataSource DataSource => _owner.DataSource;

        public object Item => _topicItem;
        /// <summary>
        /// Returns the context in which this view model is working.
        /// </summary>
        public ICloudSourceContext Context => _owner.Context;

        public event EventHandler ItemChanged;

        public TopicViewModel(PubsubSourceRootViewModel owner, Topic topic)
        {
            _owner = owner;
            _topicItem = new TopicItem(topic);
            Caption = _topicItem.Name;
            Icon = s_topicIcon.Value;
            foreach (var subscription in ListSubscriptionViewModels())
            {
                Children.Add(new SubscriptionViewModel(this, subscription));
            }
            ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerPubSubNewSubscriptionMenuHeader,
                        Command = new WeakCommand(OnNewSubscriptionCommand)
                    },
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerPubSubDeleteTopicMenuHeader,
                        Command = new WeakCommand(OnDeleteTopicCommand)
                    },
                    new MenuItem
                    {
                        Header = Resources.UiPropertiesMenuHeader,
                        Command = new WeakCommand(OnPropertiesWindowCommand)
                    }
                }
            };
        }

        private async void OnNewSubscriptionCommand()
        {
            try
            {
                IsLoading = true;
                Subscription subscription;
                if (NewSubscriptionWindowContent.PromptUser(_topicItem.FullName, out subscription))
                {
                    await DataSource.NewSubscriptionAsync(subscription);
                    Refresh();
                }
                IsLoading = false;
            }
            catch (DataSourceException e)
            {
                Debug.Write(e.Message, "New Subscription");
                UserPromptUtils.ErrorPrompt("Error creating new subscription.", "Error in new subscription");
            }
        }

        private async void OnDeleteTopicCommand()
        {
            try
            {
                IsLoading = true;
                bool doDelete = UserPromptUtils.YesNoPrompt(
                    string.Format(Resources.PubSubDeleteTopicWindowMessage, _topicItem.Name),
                    Resources.PubSubDeleteTopicWindowHeader);
                IsLoading = false;
                if (doDelete)
                {
                    await DataSource.DeleteTopicAsync(_topicItem.Name);
                    Refresh();
                }
            }
            catch (DataSourceException e)
            {
                Debug.Write(e.Message, "Delete Topic");
                UserPromptUtils.ErrorPrompt(
                    Resources.PubSubDeleteTopicErrorMessage, Resources.PubSubDeleteTopicErrorHeader);
            }
        }

        private void OnPropertiesWindowCommand()
        {
            Context.ShowPropertiesWindow(Item);
        }

        /// <summary>
        /// Gets the subscriptions of this topic.
        /// </summary>
        private IEnumerable<Subscription> ListSubscriptionViewModels()
        {
            return _owner.Subscriptions.Where(subscription => subscription.Topic == _topicItem.FullName);
        }

        public virtual void Refresh()
        {
            _owner.Refresh();
        }
    }
}
