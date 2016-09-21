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

using System;
using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE service in the Google Cloud Explorer Window.
    /// </summary>
    class ServiceViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconServiceResourcePath = "CloudExplorerSources/Gae/Resources/service_icon.png";

        private static readonly Lazy<ImageSource> s_serviceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconServiceResourcePath));

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeLoadingVersionCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeNoVersionsFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeFailedToLoadVersionsCaption,
            IsError = true
        };

        private readonly GaeSourceRootViewModel _owner;

        private bool _resourcesLoaded;

        private bool _showOnlyFlexVersions;

        private List<VersionViewModel> _versions;

        public readonly GaeSourceRootViewModel root;

        public readonly Service service;

        public event EventHandler ItemChanged;

        public object Item => GetItem();

        public bool ShowOnlyFlexVersions
        {
            get { return _showOnlyFlexVersions;  }
            set
            {
                if (value == _showOnlyFlexVersions)
                {
                    return;
                }
                _showOnlyFlexVersions = value;
                UpdateContextMenu();
                PresentViewModels();
            }
        }

        public ServiceViewModel(GaeSourceRootViewModel owner, Service service)
        {
            _owner = owner;
            this.service = service;
            root = _owner;

            Caption = service.Id;
            Icon = s_serviceIcon.Value;

            _resourcesLoaded = false;
            _showOnlyFlexVersions = true;

            Children.Add(s_loadingPlaceholder);

            UpdateContextMenu();
        }

        private void UpdateContextMenu()
        {
            var menuItems = new List<FrameworkElement>
            {
                new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new WeakCommand(OnOpenOnCloudConsoleCommand) },
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new WeakCommand(OnPropertiesWindowCommand) },
                new MenuItem { Header = Resources.CloudExplorerGaeServiceOpen, Command = new WeakCommand(OnOpenService) },
                new Separator(),
            };

            if (ShowOnlyFlexVersions)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowAllVersions, Command = new WeakCommand(OnShowAllInstances) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowFlexVersions, Command = new WeakCommand(OnShowOnlyFlexInstances) });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void PresentViewModels()
        {
            if (_versions == null)
            {
                return;
            }

            List<VersionViewModel> versions = _versions
                .Where(x => !ShowOnlyFlexVersions || (x.version.Vm ?? false))
                .ToList();
            UpdateViewModels(versions);
        }

        private void UpdateViewModels(List<VersionViewModel> versions)
        {
            Children.Clear();
            foreach (var version in versions)
            {
                Children.Add(version);
            }
            if (Children.Count == 0)
            {
                Children.Add(s_noItemsPlacehoder);
            }
        }

        private void OnShowOnlyFlexInstances()
        {
            ShowOnlyFlexVersions = true;
        }

        private void OnShowAllInstances()
        {
            ShowOnlyFlexVersions = false;
        }

        protected override async void OnIsExpandedChanged(bool newValue)
        {
            base.OnIsExpandedChanged(newValue);
            try
            {
                // If this is the first time the node has been expanded load it's resources.
                if (!_resourcesLoaded && newValue)
                {
                    _resourcesLoaded = true;
                    _versions = await LoadVersionList();
                    Children.Clear();
                    if (_versions == null)
                    {
                        Children.Add(s_errorPlaceholder);
                    }
                    else
                    {
                        PresentViewModels();
                    }
                }
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine(Resources.CloudExplorerGaeFailedVersionsMessage);
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private void OnOpenOnCloudConsoleCommand()
        {
            var url = $"https://console.cloud.google.com/appengine/versions?project={root.Context.CurrentProject.ProjectId}&moduleId={service.Id}";
            Process.Start(url);
        }

        private void OnPropertiesWindowCommand()
        {
            root.Context.ShowPropertiesWindow(Item);
        }

        private void OnOpenService()
        {
            var url = GaeUtils.GetAppUrl(root.GaeApplication.DefaultHostname, service.Id);
            Process.Start(url);
        }

        /// <summary>
        /// Load a list of flexible versions sorted by percent of traffic allocation.
        /// </summary>
        private async Task<List<VersionViewModel>> LoadVersionList()
        {
            var versions = await root.DataSource.Value.GetVersionListAsync(service.Id);
            return versions?
                .Select(x => new VersionViewModel(this, x))
                .OrderByDescending(x => GaeServiceExtensions.GetTrafficAllocation(service, x.version.Id))
                .ToList();
        }

        public ServiceItem GetItem() => new ServiceItem(service);
    }
}
