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

using Google;
using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class GcsSourceRootViewModel : SourceRootViewModelBase
    {
        private const string ComponentApiName = "storage_component";

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGcsLoadingBucketsCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGcsNoBucketsFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGcsFailedToListBucketsCaption,
            IsError = true
        };

        private static readonly IList<string> s_requiredApis = new List<string>
        {
            // The GCS API is required.
            KnownApis.CloudStorageApiName
        };

        private Lazy<GcsDataSource> _dataSource;
        private bool _showLocations = false;
        private IList<Bucket> _buckets;

        public override string RootCaption => Resources.CloudExplorerGcsRootNodeCaption;

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override TreeLeaf ApiNotEnabledPlaceholder
            => new TreeLeaf
            {
                Caption = Resources.CloudExplorerGcsApiNotEnabledCaption,
                IsError = true,
                ContextMenu = new ContextMenu
                {
                    ItemsSource = new List<FrameworkElement>
                    {
                        new MenuItem
                        {
                            Header = Resources.CloudExplorerGcsEnableApiMenuHeader,
                            Command = new ProtectedCommand(OnEnableGcsApi)
                        }
                    }
                }
            };

        public override IList<string> RequiredApis => s_requiredApis;

        public bool ShowLocations
        {
            get { return _showLocations; }
            set
            {
                if (value == _showLocations)
                {
                    return;
                }
                _showLocations = value;
                UpdateContextMenu();
                PresentViewModels();
            }
        }

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);

            InvalidateProjectOrAccount();
            UpdateContextMenu();
        }

        private void UpdateContextMenu()
        {
            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.CloudExplorerStatusMenuHeader, Command = new ProtectedCommand(OnStatusCommand) },
            };

            if (_showLocations)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGcsShowBucketsCommand, Command = new ProtectedCommand(OnShowBucketsCommand) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGcsShowLocationsCommand, Command = new ProtectedCommand(OnShowLocationsCommand) });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnShowLocationsCommand()
        {
            ShowLocations = true;
        }

        private void OnShowBucketsCommand()
        {
            ShowLocations = false;
        }

        private async void OnEnableGcsApi()
        {
            await ApiManager.Default.EnableServicesAsync(s_requiredApis);
            Refresh();
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
                return new GcsDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.VersionedApplicationName);
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

                _buckets = await LoadBucketList();
                PresentViewModels();

                EventsReporterWrapper.ReportEvent(GcsBucketsLoadedEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException ex)
            {
                var innerEx = ex.InnerException as GoogleApiException;
                if (innerEx != null && innerEx.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Debug.WriteLine("GCS API is not enabled.");

                    Children.Clear();
                    Children.Add(new DisabledApiWarning(
                        apiName: ComponentApiName,
                        caption: Resources.CloudExplorerGcsSourceApiDisabledMessage,
                        project: Context.CurrentProject));
                    return;
                }

                EventsReporterWrapper.ReportEvent(GcsBucketsLoadedEvent.Create(CommandStatus.Failure));
                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private void UpdateViewModels(IEnumerable<TreeNode> viewModels)
        {
            Children.Clear();
            foreach (var item in viewModels)
            {
                Children.Add(item);
            }
            if (Children.Count == 0)
            {
                Children.Add(s_noItemsPlacehoder);
            }
        }

        private void PresentViewModels()
        {
            IEnumerable<TreeNode> viewModels;
            if (_showLocations)
            {
                viewModels = _buckets
                    .GroupBy(x => x.Location)
                    .Select(x => new { Name = x.Key, Buckets = x.Select(b => new BucketViewModel(this, b)) })
                    .Select(x => new BucketLocationViewModel(this, x.Name, x.Buckets));
            }
            else
            {
                viewModels = _buckets.Select(x => new BucketViewModel(this, x));
            }
            UpdateViewModels(viewModels);
        }

        private Task<IList<Bucket>> LoadBucketList()
        {
            var credential = CredentialsStore.Default.CurrentGoogleCredential;
            return _dataSource.Value.GetBucketListAsync();
        }

        private void OnStatusCommand()
        {
            Process.Start("https://status.cloud.google.com/");
        }
    }
}