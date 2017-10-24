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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PubSubWindows;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Root node of the Pubsub tree.
    /// </summary>
    internal class PubsubSourceRootViewModel : SourceRootViewModelBase, IPubsubSourceRootViewModel
    {
        internal const string PubSubConsoleUrlFormat = "https://console.cloud.google.com/cloudpubsub?project={0}";
        private const string BlackListPrefix = "^projects/{0}/topics/";

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

        private static readonly IList<string> s_requiredApis = new List<string>
        {
            // Require the Pub/Sub API.
            KnownApis.PubSubApiName
        };

        private static readonly string[] s_blacklistedTopics =
        {
            "asia\\.gcr\\.io%2F{0}$",
            "eu\\.gcr\\.io%2F{0}$",
            "gcr\\.io%2F{0}$",
            "us\\.gcr\\.io%2F{0}$",
            "cloud-builds$",
            "repository-changes\\..*$"
        };

        private Lazy<IPubsubDataSource> _dataSource;
        // Mockable static methods for testing.
        private readonly Func<IPubsubDataSource> _dataSourceFactory;
        internal Func<string, string> NewTopicUserPrompt = NewTopicWindow.PromptUser;
        internal Func<string, Process> StartProcess = Process.Start;

        public IPubsubDataSource DataSource => _dataSource.Value;

        public override string RootCaption => Resources.CloudExplorerPubSubRootCaption;
        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;
        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;
        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf ApiNotEnabledPlaceholder
            => new TreeLeaf
            {
                Caption = Resources.CloudExplorerPubSubApiNotEnabledCaption,
                IsError = true,
                ContextMenu = new ContextMenu
                {
                    ItemsSource = new List<MenuItem>
                    {
                        new MenuItem
                        {
                            Header = Resources.CloudExplorerPubSubEnableApiMenuHeader,
                            Command = new ProtectedCommand(OnEnablePubSubApi)
                        }
                    }
                }
            };

        public override IList<string> RequiredApis => s_requiredApis;

        private string CurrentProjectId => Context.CurrentProject.ProjectId;

        /// <summary>
        /// Creates the pub sub source root view model.
        /// </summary>
        public PubsubSourceRootViewModel() : this(CreateDataSource)
        { }

        /// <summary>
        /// For testing.
        /// </summary>
        internal PubsubSourceRootViewModel(Func<IPubsubDataSource> dataSourceFactory)
        {
            _dataSourceFactory = dataSourceFactory;
            _dataSource = new Lazy<IPubsubDataSource>(_dataSourceFactory);
        }

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
            _dataSource = new Lazy<IPubsubDataSource>(_dataSourceFactory);
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

                EventsReporterWrapper.ReportEvent(PubSubTopicsLoadedEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException e)
            {
                EventsReporterWrapper.ReportEvent(PubSubTopicsLoadedEvent.Create(CommandStatus.Failure));
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
            return !s_blacklistedTopics.SelectMany(FormatBlacklistedTopics).Any(s => Regex.IsMatch(topic.Name, s));
        }

        /// <summary>
        /// Gets a regex to filter the blacklisted topics.
        /// </summary>
        /// <param name="blacklistedTopicString">
        /// A format string of a blacklisted topic. It may take project id as the first format arg.
        /// <example>
        /// "us\\.gcr\\.io%2F{0}$" => "^projects/ProjectId/topics/us\\.gcr\\.io%2FProjectId$"
        /// </example>
        /// </param>
        /// <returns>
        /// A regex string to match a blacklisted topic.
        /// </returns>
        private IEnumerable<string> FormatBlacklistedTopics(string blacklistedTopicString)
        {
            string escapedProjectId = Regex.Escape(CurrentProjectId);
            string restUrlPrefix = string.Format(BlackListPrefix, escapedProjectId);
            yield return restUrlPrefix + string.Format(blacklistedTopicString, escapedProjectId);
            yield return restUrlPrefix +
                string.Format(
                    blacklistedTopicString, Regex.Escape(Uri.EscapeDataString(CurrentProjectId.Replace(":", "/"))));
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
        internal void OnOpenCloudConsoleCommand()
        {
            var url = string.Format(PubSubConsoleUrlFormat, CurrentProjectId);
            StartProcess(url);
        }

        /// <summary>
        /// Opens the new pub sub topic dialog.
        /// </summary>
        internal async void OnNewTopicCommand()
        {
            try
            {
                string topicName = NewTopicUserPrompt(CurrentProjectId);
                if (topicName != null)
                {
                    await DataSource.NewTopicAsync(topicName);
                    Refresh();

                    EventsReporterWrapper.ReportEvent(PubSubTopicCreatedEvent.Create(CommandStatus.Success));
                }
            }
            catch (DataSourceException e)
            {
                Debug.Write(e.Message, "New Topic");
                UserPromptUtils.ErrorPrompt(
                    Resources.PubSubNewTopicErrorMessage, Resources.PubSubNewTopicErrorHeader, e.Message);

                EventsReporterWrapper.ReportEvent(PubSubTopicCreatedEvent.Create(CommandStatus.Failure));
            }
        }

        private async void OnEnablePubSubApi()
        {
            await ApiManager.Default.EnableServicesAsync(s_requiredApis);
            Refresh();
        }
    }
}
