using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    internal class PubSubTopicViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/bucket_icon.png";
        private static readonly Lazy<ImageSource> s_topicIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));
        private readonly Lazy<PubSubTopicItem> _item;
        private PubSubSourceRootViewModel _owner;
        public object Item => _item.Value;
        public event EventHandler ItemChanged;

        public PubSubTopicViewModel(PubSubSourceRootViewModel owner, Topic topic)
        {
            _owner = owner;
            _item = new Lazy<PubSubTopicItem>(() => new PubSubTopicItem(topic));

            Caption = topic.Name;
            Icon = s_topicIcon.Value;
        }
    }
}
