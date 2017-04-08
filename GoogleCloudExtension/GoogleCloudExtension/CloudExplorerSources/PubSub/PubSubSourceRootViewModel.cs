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
using GoogleCloudExtension.Accounts;
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

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Root node of the Pubsub tree.
    /// </summary>
    internal class PubsubSourceRootViewModel : SourceRootViewModelBase
    {
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubLoadingTopicsCaption,
            IsLoading = true
        };

        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubNoTopicsFoundCaption,
            IsWarning = true
        };

        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubListTopicsErrorCaption,
            IsError = true
        };

        private static readonly string[] s_blacklistedTopics =
        {
            "asia.gcr.io%2F{0}", "eu.gcr.io%2F{0}",
            "gcr.io%2F{0}", "us.gcr.io%2F{0}"
        };

        private Lazy<PubsubDataSource> _dataSource = new Lazy<PubsubDataSource>(CreateDataSource);

        public PubsubDataSource DataSource => _dataSource.Value;

        public override string RootCaption => Resources.CloudExplorerPubSubRootCaption;
        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;
        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;
        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        private string CurrentProjectId => Context.CurrentProject.ProjectId;
        private string BlackListPrefix => $"projects/{CurrentProjectId}/topics/";

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);

            ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerPubSubNewTopicMenuHeader,
                        Command = new ProtectedCommand(OnNewTopicCommand)
                    },
                    new MenuItem
                    {
                        Header = Resources.UiOpenOnCloudConsoleMenuHeader,
                        Command = new ProtectedCommand(OnOpenCloudConsoleCommand)
                    }
                }
            };
        }

        /// <summary>
        /// Resets the datasource.
        /// </summary>
        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating the Pubsub source.");
            _dataSource = new Lazy<PubsubDataSource>(CreateDataSource);
        }

        /// <summary>
        /// Loads the topics, and creates child topic nodes.
        /// </summary>
        protected override async Task LoadDataOverride()
        {
            try
            {
                Task<IList<Subscription>> subscriptionsTask = DataSource.GetSubscriptionListAsync();
                IEnumerable<Topic> topics = await DataSource.GetTopicListAsync();
                IList<Subscription> subscriptions = await subscriptionsTask;
                Children.Clear();
                foreach (Topic topic in topics.Where(IsIncludedTopic))
                {
                    Children.Add(new TopicViewModel(this, topic, subscriptions));
                }
                if (subscriptions.Any(s => s.Topic.Equals(OrphanedSubscriptionsItem.DeletedTopicName)))
                {
                    Children.Add(new OrphanedSubscriptionsViewModel(this, subscriptions));
                }

            }
            catch (DataSourceException e)
            {
                throw new CloudExplorerSourceException(e.Message, e);
            }
        }

        /// <summary>
        /// Checks the topic against a blacklist of topics not to display.
        /// </summary>
        /// <param name="topic">The topic to check.</param>
        /// <returns>True if the topic is not blacklisted.</returns>
        private bool IsIncludedTopic(Topic topic)
        {
            return !s_blacklistedTopics.Select(FormatBlacklistedTopics).Any(topic.Name.Equals);
        }

        /// <summary>
        /// Gets the full formatted name of a blacklisted topic given a blacklisted topic format string.
        /// </summary>
        /// <param name="blacklistedTopicString">
        /// A format string of a blacklisted topic. Needs a project id as input.
        /// </param>
        /// <returns>
        /// The full name of a blacklisted topic, including prefix and formatted with the project id.
        /// </returns>
        private string FormatBlacklistedTopics(string blacklistedTopicString)
        {
            return BlackListPrefix + string.Format(blacklistedTopicString, CurrentProjectId);
        }

        /// <summary>
        /// Creates a new PubsubDataSource from the default credentials.
        /// </summary>
        private static PubsubDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new PubsubDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.ApplicationName);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Opens the google pub sub cloud console.
        /// </summary>
        private void OnOpenCloudConsoleCommand()
        {
            var url = $"https://console.cloud.google.com/cloudpubsub?project={CurrentProjectId}";
            Process.Start(url);
        }

        /// <summary>
        /// Opens the new pub sub topic dialog.
        /// </summary>
        private async void OnNewTopicCommand()
        {
            try
            {
                string topicName = NewTopicWindow.PromptUser(CurrentProjectId);
                if (topicName != null)
                {
                    await DataSource.NewTopicAsync(topicName);
                    Refresh();
                }
            }
            catch (DataSourceException e)
            {
                Debug.Write(e.Message, "New Topic");
                UserPromptUtils.ErrorPrompt(
                    Resources.PubSubNewTopicErrorMessage, Resources.PubSubNewTopicErrorHeader);
            }
        }
    }
}
