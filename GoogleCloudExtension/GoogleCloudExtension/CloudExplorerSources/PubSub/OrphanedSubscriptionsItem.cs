using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Topic like item describing the container of orphaned subscriptions.
    /// </summary>
    internal class OrphanedSubscriptionsItem : ITopicItem
    {
        public const string DeletedTopicName = "_deleted-topic_";

        /// <summary>
        /// Display name of the orphaned subscriptions item.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubTopicCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubTopicNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubTopicNameDisplayName))]
        public string Name => Resources.PubSubOrphanedSubscriptionsItemName;

        /// <summary>
        /// Name of the topic orphaned subscriptions claim to belong to.
        /// </summary>
        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubTopicCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubTopicFullNameDescription))]
        [LocalizedDisplayName(nameof(Resources.CloudExplorerPubSubTopicFullNameDisplayName))]
        public string FullName => DeletedTopicName;
    }
}