// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceSourceRootViewModel : TreeHierarchy
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
        private static readonly TreeLeaf s_noZonesPlaceholder = new TreeLeaf { Content = "No zones" };
        private static readonly TreeLeaf s_noGcloudPlaceholder = new TreeLeaf
        {
            Content = "Please install the Google Cloud SDK.",
            IsError = true
        };

        private bool _loading = false;
        private bool _loaded = false;
        private bool _showOnlyWindowsInstances = false;
        private IList<GceInstance> _instances;

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

        public GceSourceRootViewModel()
        {
            Content = "Google Compute Engine";
            IsExpanded = false;
            Icon = s_gceIcon.Value;
            Children.Add(s_loadingPlaceholder);
        }

        protected override void OnIsExpandedChanged(bool newValue)
        {
            if (_loading)
            {
                return;
            }

            if (newValue && !_loaded)
            {
                LoadInstances();
            }
        }

        private async void LoadInstances()
        {
            _loading = true;
            try
            {
                var gcloudValidationResult = await EnvironmentUtils.ValidateGCloudInstallation();
                if (!gcloudValidationResult.IsValidGCloudInstallation())
                {
                    Children.Clear();
                    Children.Add(s_noGcloudPlaceholder);
                    _loaded = true;
                }
                else
                {
                    _instances = await LoadGceInstances();
                    _loaded = true;
                    PresentZoneViewModels();
                }
            }
            finally
            {
                _loading = false;
            }
        }

        private void PresentZoneViewModels()
        {
            if (!_loaded)
            {
                return;
            }

            var zones = GetZoneViewModels();
            Children.Clear();
            if (zones == null)
            {
                Children.Add(s_errorPlaceholder);
            }
            else
            {
                foreach (var zone in zones)
                {
                    Children.Add(zone);
                }
                if (Children.Count == 0)
                {
                    Children.Add(s_noZonesPlaceholder);
                }
            }
        }

        private async Task<IList<GceInstance>> LoadGceInstances()
        {
            var currentCredentials = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
            var oauthToken = await GCloudWrapper.Instance.GetAccessTokenAsync();
            return await GceDataSource.GetInstanceListAsync(currentCredentials.ProjectId, oauthToken);
        }

        private IList<ZoneViewModel> GetZoneViewModels()
        {
            return _instances?
                .Where(x => !_showOnlyWindowsInstances || x.IsWindowsInstance())
                .GroupBy(x => x.ZoneName)
                .Select(x => new ZoneViewModel(x.Key, x)).ToList();
        }

        internal void Refresh()
        {
            if (!_loaded)
            {
                return;
            }

            _loaded = false;
            ResetChildren();
            LoadInstances();
        }

        private void ResetChildren()
        {
            Children.Clear();
            Children.Add(s_loadingPlaceholder);
        }
    }
}