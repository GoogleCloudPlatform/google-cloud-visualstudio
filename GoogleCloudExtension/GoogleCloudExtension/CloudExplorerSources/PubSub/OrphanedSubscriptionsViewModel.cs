using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Cloud explorer node for the container for orphaned subscriptions.
    /// </summary>
    internal class OrphanedSubscriptionsViewModel : TopicViewModelBase
    {

        private const string IconResourcePath = "CloudExplorerSources/PubSub/Resources/orphaned_subscriptions_icon.png";

        private static readonly Lazy<ImageSource> s_orphanedSubscriptionIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        public OrphanedSubscriptionsViewModel(PubsubSourceRootViewModel owner, IEnumerable<Subscription> subscriptions)
            : base(owner, new OrphanedSubscriptionsItem(), subscriptions)
        {
            Icon = s_orphanedSubscriptionIcon.Value;
        }
    }
}
