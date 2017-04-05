using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.CloudExplorer;
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
    internal abstract class TopicViewModelBase : TreeHierarchy, ICloudExplorerItemSource
    {
        protected PubsubSourceRootViewModel Owner { get; }
        protected ITopicItem TopicItem { get; }
        private bool _isRefreshing;

        protected TopicViewModelBase(
            PubsubSourceRootViewModel owner,
            ITopicItem topicItem,
            IEnumerable<Subscription> subscriptions)
        {
            Owner = owner;
            TopicItem = topicItem;
            Caption = TopicItem.Name;
            AddSubscriptonsOfTopic(subscriptions);
        }

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

        /// <summary>
        /// The PubsubDataSource to connect to.
        /// </summary>
        public PubsubDataSource DataSource => Owner.DataSource;

        /// <summary>
        /// The topic item of this view model.
        /// </summary>
        public object Item => TopicItem;

        /// <summary>
        /// Returns the context in which this view model is working.
        /// </summary>
        public ICloudSourceContext Context => Owner.Context;

        public event EventHandler ItemChanged;

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
        /// Adds the child subscriptions from the enumeration of all subscriptions.
        /// </summary>
        /// <param name="subscriptions">An enumeration of all subscriptions.</param>
        private void AddSubscriptonsOfTopic(IEnumerable<Subscription> subscriptions)
        {
            Func<Subscription, bool> isTopicMemeber = subscription => subscription.Topic == TopicItem.FullName;
            foreach (Subscription subscription in subscriptions.Where(isTopicMemeber))
            {
                Children.Add(new SubscriptionViewModel(this, subscription));
            }
        }
    }
}