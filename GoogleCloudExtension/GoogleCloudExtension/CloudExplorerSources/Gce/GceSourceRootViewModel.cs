using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
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
        const string IconResourcePath = "CloudExplorerSources/Gce/Resources/gce_logo.png";

        private static readonly Lazy<ImageSource> s_gceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Content = "Loading instances...",
            IsLoading = true
        };

        private bool _loading = false;
        private bool _loaded = false;

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
                var zones = await LoadZones();
                Children.Clear();
                if (zones != null)
                {
                    foreach (var zone in zones)
                    {
                        Children.Add(zone);
                    }
                }
                if (Children.Count == 0)
                {
                    Children.Add(new TreeLeaf { Content = "No zones" });
                }
                _loaded = true;
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task<IList<ZoneViewModel>> LoadZones()
        {
            var currentCredentials = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
            var oauthToken = await GCloudWrapper.Instance.GetAccessTokenAsync();
            var instances = await GceDataSource.GetInstanceListAsync(currentCredentials.ProjectId, oauthToken);
            return instances?.GroupBy(x => x.ZoneName).Select(x => new ZoneViewModel(x.Key, x)).ToList();
        }

        internal void Refresh()
        {
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