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

        private Lazy<PubsubDataSource> _dataSource = new Lazy<PubsubDataSource>(CreateDataSource);
        public PubsubDataSource DataSource => _dataSource.Value;

        public override string RootCaption => Resources.CloudExplorerPubSubRootCaption;
        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;
        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;
        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

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
                foreach (Topic topic in topics)
                {
                    Children.Add(new TopicViewModel(this, topic, subscriptions));
                }
            }
            catch (DataSourceException e)
            {
                throw new CloudExplorerSourceException(e.Message, e);
            }
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
            var url = $"https://console.cloud.google.com/cloudpubsub?project={Context.CurrentProject.ProjectId}";
            Process.Start(url);
        }

        /// <summary>
        /// Opens the new pub sub topic dialog.
        /// </summary>
        private async void OnNewTopicCommand()
        {
            try
            {
                string topicName = NewTopicWindow.PromptUser(CredentialsStore.Default.CurrentProjectId);
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
