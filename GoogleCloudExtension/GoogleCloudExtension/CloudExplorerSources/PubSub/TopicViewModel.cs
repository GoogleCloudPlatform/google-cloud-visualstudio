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
                var data = new NewSubscriptionData(_topicItem.FullName, _owner.Context.CurrentProject.ProjectId);
                var dialog = new NewSubscriptionWindow(data);
                if (dialog.ShowDialog() == true)
                {
                    await DataSource.NewSubscriptionAsync(
                        data.FullName, data.TopicFullName, data.AckDeadlineSeconds, data.Push, data.PushUrl);
                    Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                Debug.Write(e.StackTrace);
                UserPromptUtils.ErrorPrompt("Error creating new subscription.", "Error in new subscription");
            }
        }

        private async void OnDeleteTopicCommand()
        {
            bool doDelete = UserPromptUtils.YesNoPrompt(
                string.Format(Resources.PubSubDeleteTopicWindowMessage, _topicItem.Name),
                Resources.PubSubDeleteTopicWindowHeader);
            if (doDelete)
            {
                try
                {
                    await DataSource.DeleteTopicAsync(_topicItem.FullName);
                    Refresh();
                }
                catch (Exception e)
                {
                    Debug.Write(e, "Error in delete topic");
                    UserPromptUtils.ErrorPrompt(
                        Resources.PubSubDeleteTopicErrorMessage, Resources.PubSubDeleteTopicErrorHeader);

                }
            }
        }

        private void OnPropertiesWindowCommand()
        {
            _owner.Context.ShowPropertiesWindow(Item);
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
