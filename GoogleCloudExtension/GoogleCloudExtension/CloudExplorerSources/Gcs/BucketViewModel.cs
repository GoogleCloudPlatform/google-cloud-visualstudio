// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class BucketViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/bucket_icon.png";
        private static readonly Lazy<ImageSource> s_bucketIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly GcsSourceRootViewModel _owner;
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

        public event EventHandler ItemChanged;

        public BucketViewModel(GcsSourceRootViewModel owner, Bucket bucket)
        {
            _owner = owner;
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
            var url = $"https://pantheon.corp.google.com/storage/browser/{_bucket.Name}/?project={_owner.Owner.CurrentProject.ProjectId}";
            Debug.WriteLine($"Starting bucket browsing at: {url}");
            Process.Start(url);
        }

        private BucketItem GetItem() => new BucketItem(_bucket);
    }
}