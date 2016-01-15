using System;
using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class BucketViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
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
        }

        private BucketItem GetItem()
        {
            return new BucketItem(_bucket);
        }
    }
}