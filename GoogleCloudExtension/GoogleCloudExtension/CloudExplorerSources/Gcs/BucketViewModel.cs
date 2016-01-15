using System;
using GoogleCloudExtension.CloudExplorer;
using System.Windows.Media;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class BucketViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private static readonly Lazy<ImageSource> s_bucketIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly Bucket _bucket;
        private readonly Lazy<BucketItem> _item;

        public object Item
        {
            get
            {
                return _item.Value;
            }
        }

        public BucketViewModel(Bucket bucket)
        {
            _bucket = bucket;
            _item = new Lazy<BucketItem>(GetItem);

            Content = _bucket.Name;
            Icon = s_bucketIcon.Value;
        }

        private BucketItem GetItem()
        {
            return new BucketItem(_bucket);
        }
    }
}