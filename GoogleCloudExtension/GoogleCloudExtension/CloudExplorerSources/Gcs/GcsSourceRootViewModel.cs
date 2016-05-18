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
        private static readonly Lazy<ImageSource> s_storageIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = "Loading buckets...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = "No buckets found."
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = "Failed to list buckets.",
            IsError = true
        };

        private Lazy<GcsDataSource> _dataSource;

        public override ImageSource RootIcon => s_storageIcon.Value;

        public override string RootCaption => "Google Cloud Storage";

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override void Initialize()
        {
            base.Initialize();

            InvalidateProjectOrAccount();
        }

        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating the GCS source.");
            _dataSource = new Lazy<GcsDataSource>(CreateDataSource);
        }

        private GcsDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new GcsDataSource(CredentialsStore.Default.CurrentProjectId, CredentialsStore.Default.CurrentGoogleCredential);
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
            var credential = CredentialsStore.Default.CurrentGoogleCredential;
            var buckets = await _dataSource.Value.GetBucketListAsync();
            return buckets?.Select(x => new BucketViewModel(this, x)).ToList();
        }
    }
}