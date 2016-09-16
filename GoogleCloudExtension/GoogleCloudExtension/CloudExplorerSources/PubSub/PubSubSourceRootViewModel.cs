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

using Google;
using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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

        public override string RootCaption => Resources.CloudExplorerPubSubRootCaption;
        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;
        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;
        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public IList<Subscription> Subscriptions { get; private set; }

        internal PubsubDataSource DataSource => _dataSource.Value;
        private Lazy<PubsubDataSource> _dataSource = new Lazy<PubsubDataSource>(CreateDataSource);

        public PubsubSourceRootViewModel()
        {
            Subscriptions = new Subscription[0];
        }

        /// <summary>
        /// Creates a new PubsubDataSource from the default credentials.
        /// </summary>
        private static PubsubDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                var credential = CredentialsStore.Default.CurrentGoogleCredential;
                if (credential.IsCreateScopedRequired)
                {
                    credential.CreateScoped(PubsubService.Scope.Pubsub);
                }
                return new PubsubDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    credential,
                    GoogleCloudExtensionPackage.ApplicationName);
            }
            else
            {
                return null;
            }
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
                Task<IList<Subscription>> subscriptionsTask = GetSubscriptionsAsync();
                IEnumerable<Topic> topics = await DataSource.GetTopicListAsync();
                Subscriptions = await subscriptionsTask;
                Children.Clear();
                foreach (var topic in topics)
                {
                    Children.Add(new TopicViewModel(this, topic));
                }
            }
            catch (GoogleApiException e)
            {
                throw new CloudExplorerSourceException(e.Message, e);
            }
        }


        private Task<IList<Subscription>> GetSubscriptionsAsync()
        {
            return DataSource.GetSubscriptionListAsync();
        }
    }
}
