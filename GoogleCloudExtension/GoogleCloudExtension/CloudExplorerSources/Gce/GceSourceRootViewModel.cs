// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

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

        private static readonly Lazy<ImageSource> s_gceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Content = "Loading instances...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Content = "Failed to load instances.",
            IsError = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Content = "No instances found."
        };
        private static readonly TreeLeaf s_noZonesPlaceholder = new TreeLeaf { Content = "No zones" };

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

        public override void Initialize(ICloudExplorerSource owner)
        {
            base.Initialize(owner);

            InvalidateCredentials();
        }

        public override void InvalidateCredentials()
        {
            Debug.WriteLine("New credentials, invalidating data source for GCE");
            _dataSource = new Lazy<GceDataSource>(CreateDataSource); 
        }

        private GceDataSource CreateDataSource()
        {
            if (Owner.CurrentProject != null)
            {
                return new GceDataSource(Owner.CurrentProject.ProjectId, AccountsManager.CurrentGoogleCredential);
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
                .GroupBy(x => x.ZoneName())
                .Select(x => new ZoneViewModel(this, x.Key, x)).ToList();
        }
    }
}