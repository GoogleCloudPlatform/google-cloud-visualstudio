// // Copyright 2016 Google Inc. All Rights Reserved.
// //
// // Licensed under the Apache License, Version 2.0 (the "License");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    internal class PubSubSourceRootViewModel : SourceRootViewModelBase
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

        public override string RootCaption { get; } = Resources.CloudExplorerPubSubRootCaption;
        public override TreeLeaf ErrorPlaceholder { get; } = s_errorPlaceholder;
        public override TreeLeaf NoItemsPlaceholder { get; } = s_noItemsPlacehoder;
        public override TreeLeaf LoadingPlaceholder { get; } = s_loadingPlaceholder;

        private Lazy<PubSubDataSource> _dataSource = new Lazy<PubSubDataSource>(CreateDataSource);
        internal PubSubDataSource DataSource => _dataSource.Value;

        private Lazy<Task<IList<Subscription>>> _subscriptions;
        public Task<IList<Subscription>> Subscriptions => _subscriptions.Value;

        public PubSubSourceRootViewModel()
        {
            _subscriptions = new Lazy<Task<IList<Subscription>>>(GetSubscriptionsAsync);
        }

        private static PubSubDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                var credential = CredentialsStore.Default.CurrentGoogleCredential;
                if (credential.IsCreateScopedRequired)
                {
                    credential.CreateScoped(PubsubService.Scope.Pubsub);
                }
                return new PubSubDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    credential,
                    GoogleCloudExtensionPackage.ApplicationName);
            }
            else
            {
                return null;
            }
        }

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);
        }

        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating the GCS source.");
            _dataSource = new Lazy<PubSubDataSource>(CreateDataSource);
        }

        protected override async Task LoadDataOverride()
        {
            try
            {
                _subscriptions = new Lazy<Task<IList<Subscription>>>(GetSubscriptionsAsync);
                IEnumerable<TopicViewModel> topicModels = await ListTopicViewModelsAsync();
                Children.Clear();
                foreach (var topicModel in topicModels)
                {
                    Children.Add(topicModel);
                }
            }
            catch (Exception e)
            {
                Children.Add(new TreeLeaf { Caption = e.Message, IsError = true });
                throw new CloudExplorerSourceException(e.Message, e);
            }
        }

        private async Task<IEnumerable<TopicViewModel>> ListTopicViewModelsAsync()
        {
            IEnumerable<Topic> topics = await _dataSource.Value.GetTopicListAsync();
            return topics.Select(topic => new TopicViewModel(this, topic));
        }

        private Task<IList<Subscription>> GetSubscriptionsAsync()
        {
            return _dataSource.Value.GetSubscriptionListAsync();
        }
    }
}
