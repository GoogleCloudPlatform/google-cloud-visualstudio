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

using Google.Apis.Compute.v1.Data;
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

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceSourceRootViewModel : SourceRootViewModelBase
    {
        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/gce_logo.png";

        private static readonly Lazy<ImageSource> s_gceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = "Loading instances...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = "Failed to load instances.",
            IsError = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = "No instances found."
        };
        private static readonly TreeLeaf s_noZonesPlaceholder = new TreeLeaf { Caption = "No zones" };

        private bool _showOnlyWindowsInstances = false;
        private IList<Instance> _instances;
        private Lazy<GceDataSource> _dataSource;

        public GceDataSource DataSource => _dataSource.Value;

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override string RootCaption => "Google Compute Engine";

        public override ImageSource RootIcon => s_gceIcon.Value;

        public bool ShowOnlyWindowsInstances
        {
            get { return _showOnlyWindowsInstances; }
            set
            {
                if (value == _showOnlyWindowsInstances)
                {
                    return;
                }
                _showOnlyWindowsInstances = value;

                PresentZoneViewModels();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            InvalidateProjectOrAccount();
        }

        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating data source for GCE");
            _dataSource = new Lazy<GceDataSource>(CreateDataSource);
        }

        private GceDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new GceDataSource(CredentialsStore.Default.CurrentProjectId, CredentialsStore.Default.CurrentGoogleCredential);
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
                _instances = await LoadGceInstances();
                PresentZoneViewModels();
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine("Failed to load the list of Gce instances.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private void PresentZoneViewModels()
        {
            if (_instances == null)
            {
                return;
            }

            var zones = GetZoneViewModels();
            Children.Clear();
            foreach (var zone in zones)
            {
                Children.Add(zone);
            }
            if (Children.Count == 0)
            {
                Children.Add(s_noZonesPlaceholder);
            }
        }

        private async Task<IList<Instance>> LoadGceInstances()
        {
            return await _dataSource.Value.GetInstanceListAsync();
        }

        private IList<ZoneViewModel> GetZoneViewModels()
        {
            return _instances?
                .Where(x => !_showOnlyWindowsInstances || x.IsWindowsInstance())
                .GroupBy(x => x.GetZoneName())
                .Select(x => new ZoneViewModel(this, x.Key, x)).ToList();
        }
    }
}