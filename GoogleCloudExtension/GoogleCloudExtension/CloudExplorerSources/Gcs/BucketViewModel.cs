using System;
using GoogleCloudExtension.CloudExplorer;
using System.Windows.Media;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Windows.Controls;
using GoogleCloudExtension.GCloud;
using System.Diagnostics;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class BucketViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private static readonly Lazy<ImageSource> s_bucketIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly Bucket _bucket;
        private readonly Lazy<BucketItem> _item;
        private readonly WeakCommand _openBucketCommand;

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
            _openBucketCommand = new WeakCommand(OnOpenBucket);

            Content = _bucket.Name;
            Icon = s_bucketIcon.Value;

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = "Browse Bucket", Command = _openBucketCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private async void OnOpenBucket()
        {
            var currentCredentials = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
            var url = $"https://pantheon.corp.google.com/storage/browser/{_bucket.Name}/?project={currentCredentials.ProjectId}";
            Debug.WriteLine($"Starting bucket browsing at: {url}");
            Process.Start(url);
        }

        private BucketItem GetItem()
        {
            return new BucketItem(_bucket);
        }
    }
}