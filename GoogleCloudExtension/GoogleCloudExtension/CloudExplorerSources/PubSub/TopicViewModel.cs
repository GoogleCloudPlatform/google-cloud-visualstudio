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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    internal class TopicViewModel : SourceRootViewModelBase
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/bucket_icon.png";
        private static readonly Lazy<ImageSource> s_topicIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));
        private readonly Lazy<TopicItem> _item;
        private PubSubSourceRootViewModel _owner;

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubLoadingSubscriptionsCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubNoTopicsFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubListSubscriptionsErrorCaption,
            IsError = true
        };

        public object Item => _item.Value;
        public event EventHandler ItemChanged;

        public TopicViewModel(PubSubSourceRootViewModel owner, Topic topic)
        {
            _owner = owner;
            _item = new Lazy<TopicItem>(() => new TopicItem(topic));

            Caption = topic.Name;
            RootCaption = topic.Name;
            Icon = s_topicIcon.Value;
        }

        public override string RootCaption { get; }
        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;
        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;
        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;
        protected override async Task LoadDataOverride()
        {
            IList<SubscriptionViewModel> subscriptionViewModels = await ListSubscriptionViewModelsAsync();
            Children.Clear();
            foreach (var subscriptionViewModel in subscriptionViewModels)
            {
                Children.Add(subscriptionViewModel);
            }
        }

        private async Task<IList<SubscriptionViewModel>> ListSubscriptionViewModelsAsync()
        {
            Task<IList<string>> topicSubscriptionNamesTask =
                _owner.DataSource.GetTopicSubscriptionListAsync(_item.Value.Name);
            IList<Subscription> allSubscriptions = await _owner.Subscriptions;
            IList<string> topicSubscriptionNamesList = await topicSubscriptionNamesTask;
            ISet<string> topicSubscriptionNames = new HashSet<string>(topicSubscriptionNamesList);
            var topicSubscriptions = new List<SubscriptionViewModel>();
            foreach (var subscription in allSubscriptions)
            {
                if (topicSubscriptionNames.Contains(subscription.Name))
                {
                    topicSubscriptions.Add(new SubscriptionViewModel(this, subscription));
                }
            }
            return topicSubscriptions;
        }

        protected virtual void OnItemChanged()
        {
            ItemChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
