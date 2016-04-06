// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Credentials;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class GcsSourceRootViewModel : SourceRootViewModelBase
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/storage.png";
        private static readonly Lazy<ImageSource> s_storageIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Content = "Loading buckets...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Content = "No buckets found."
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Content = "Failed to list buckets.",
            IsError = true
        };

        public override ImageSource RootIcon => s_storageIcon.Value;

        public override string RootCaption => "Google Cloud Storage";

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        protected override async Task LoadDataOverride()
        {
            try
            {
                Debug.WriteLine("Loading list of buckets.");
                var buckets = await LoadBucketList();
                Children.Clear();
                if (buckets == null)
                {
                    Children.Add(s_errorPlaceholder);
                }
                else
                {
                    foreach (var item in buckets)
                    {
                        Children.Add(item);
                    }
                    if (Children.Count == 0)
                    {
                        Children.Add(s_noItemsPlacehoder);
                    }
                }
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine("Failed to load the list of GCS buckets.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private async Task<List<BucketViewModel>> LoadBucketList()
        {
            var oauthToken = await CredentialsManager.GetAccessTokenAsync();
            var buckets = await GcsDataSource.GetBucketListAsync(Owner.CurrentProject.Id, oauthToken);
            return buckets?.Select(x => new BucketViewModel(this, x)).ToList();
        }
    }
}