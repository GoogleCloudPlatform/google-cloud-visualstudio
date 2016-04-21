// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.Storage.v1.Data;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class BucketItem
    {
        private const string Category = "Bucket Properties";

        private readonly Bucket _bucket;

        public BucketItem(Bucket bucket)
        {
            _bucket = bucket;
        }

        [Category(Category)]
        [Description("The name of the bucket")]
        public string Name => _bucket.Name;

        [Category(Category)]
        [Description("The creation time stamp for the bucket")]
        public string Created => _bucket.TimeCreated?.ToShortDateString();

        [Category(Category)]
        [Description("The modification time stamp for the bucket")]
        public string Updated => _bucket.Updated?.ToShortDateString();

        [Category(Category)]
        [Description("The url to the bucket")]
        public string SelfLink => _bucket.SelfLink;

        [Category(Category)]
        [Description("Whether versioning is enabled for the bucket")]
        public bool VersioningEnabled => _bucket?.Versioning?.Enabled ?? false;

        [Category(Category)]
        [Description("The storage class for the bucket")]
        public string StorageClass => _bucket.StorageClass;
    }
}