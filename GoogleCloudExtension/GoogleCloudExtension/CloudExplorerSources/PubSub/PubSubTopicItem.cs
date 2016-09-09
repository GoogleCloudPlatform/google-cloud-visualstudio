using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    internal class PubSubTopicItem
    {
        private readonly Topic _topic;

        public PubSubTopicItem(Topic topic)
        {
            _topic = topic;
        }

        [LocalizedCategory(nameof(Resources.CloudExplorerPubSubTopicCategory))]
        [LocalizedDescription(nameof(Resources.CloudExplorerPubSubTopicNameDescription))]
        public string Name => _topic.Name;
    }
}