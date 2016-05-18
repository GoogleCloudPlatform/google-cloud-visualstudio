// Copyright 2016 Google Inc. All Rights Reserved.
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

using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
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
        private static readonly Lazy<ImageSource> s_bucketIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

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

            Caption = _bucket.Name;
            Icon = s_bucketIcon.Value;

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = "Browse Bucket", Command = _openBucketCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private async void OnOpenBucket()
        {
            ExtensionAnalytics.ReportCommand(CommandName.OpenWebsiteForGcsBucket, CommandInvocationSource.Button);

            var url = $"https://console.cloud.google.com/storage/browser/{_bucket.Name}/?project={CredentialsStore.Default.CurrentProjectId}";
            Debug.WriteLine($"Starting bucket browsing at: {url}");
            Process.Start(url);
        }

        private BucketItem GetItem() => new BucketItem(_bucket);
    }
}