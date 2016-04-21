// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
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

        private Lazy<GcsDataSource> _dataSource;

        public override ImageSource RootIcon => s_storageIcon.Value;

        public override string RootCaption => "Google Cloud Storage";

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override void Initialize(ICloudExplorerSource owner)
        {
            base.Initialize(owner);

            InvalidateCredentials();
        }

        public override void InvalidateCredentials()
        {
            Debug.WriteLine("New credentials, invalidating the GCS source.");
            _dataSource = new Lazy<GcsDataSource>(CreateDataSource);
        }

        private GcsDataSource CreateDataSource()
        {
            if (Owner.CurrentProject != null)
            {
                return new GcsDataSource(Owner.CurrentProject.ProjectId, AccountsManager.CurrentGoogleCredential);
            }
            else
            {
                return null;
            }
        }

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
            var credential = AccountsManager.CurrentGoogleCredential;
            var buckets = await _dataSource.Value.GetBucketListAsync();
            return buckets?.Select(x => new BucketViewModel(this, x)).ToList();
        }
    }
}