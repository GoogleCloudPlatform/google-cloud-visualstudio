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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Base class for Topic like view models.
    /// </summary>
    internal abstract class TopicViewModelBase : TreeHierarchy, ITopicViewModelBase
    {
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubListSubscriptionsErrorCaption,
            IsError = true
        };

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubLoadingSubscriptionsCaption,
            IsLoading = true
        };

        private bool _isRefreshing;

        /// <summary>
        /// The PubsubDataSource to connect to.
        /// </summary>
        public IPubsubDataSource DataSource => Owner.DataSource;

        /// <summary>
        /// Returns the context in which this view model is working.
        /// </summary>
        public ICloudSourceContext Context => Owner.Context;

        /// <summary>
        /// The topic item of this view model.
        /// </summary>
        object ICloudExplorerItemSource.Item => Item;

        protected IPubsubSourceRootViewModel Owner { get; }

        public ITopicItem Item { get; }

        event EventHandler ICloudExplorerItemSource.ItemChanged
        {
            add { }
            remove { }
        }

        protected TopicViewModelBase(
            IPubsubSourceRootViewModel owner,
            ITopicItem item,
            IEnumerable<Subscription> subscriptions)
        {
            Owner = owner;
            Item = item;
            Caption = Item.DisplayName;
            AddSubscriptonsOfTopic(subscriptions);
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
                Children.Add(s_loadingPlaceholder);
                IList<Subscription> subscriptions = await DataSource.GetSubscriptionListAsync();
                Children.Clear();
                if (subscriptions != null)
                {
                    AddSubscriptonsOfTopic(subscriptions);
                }
            }
            catch (DataSourceException e)
            {
                Debug.Write(e, "Refresh Subscriptions");
                Children.Clear();
                Children.Add(s_errorPlaceholder);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// Adds the child subscriptions from the enumeration of all subscriptions.
        /// </summary>
        /// <param name="subscriptions">An enumeration of all subscriptions.</param>
        private void AddSubscriptonsOfTopic(IEnumerable<Subscription> subscriptions)
        {
            foreach (Subscription subscription in subscriptions.Where(s => s.Topic == Item.FullName))
            {
                Children.Add(new SubscriptionViewModel(this, subscription));
            }
        }
    }
}