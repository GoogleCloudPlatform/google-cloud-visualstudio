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

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE service in the Google Cloud Explorer Window.
    /// </summary>
    class ServiceViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
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

        public readonly GaeSourceRootViewModel root;

        public readonly Service service;

        public event EventHandler ItemChanged;

        public object Item => GetItem();

        public ServiceViewModel(GaeSourceRootViewModel owner, Service service)
        {
            _owner = owner;
            this.service = service;
            root = _owner;

            Caption = service.Id;

            _resourcesLoaded = false;
            Children.Add(s_loadingPlaceholder);

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new WeakCommand(OnOpenOnCloudConsoleCommand) },
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new WeakCommand(OnPropertiesWindowCommand) },
                new MenuItem { Header = Resources.CloudExplorerGaeServiceOpen, Command = new WeakCommand(OnOpenService) },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
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
                    var versions = await LoadVersionList();
                    Children.Clear();
                    if (versions == null)
                    {
                        Children.Add(s_errorPlaceholder);
                    }
                    else
                    {
                        foreach (var item in versions)
                        {
                            Children.Add(item);
                        }
                        if (Children.Count == 0)
                        {
                            Children.Add(s_noItemsPlacehoder);
                        }
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
        /// Load a list of versions sorted by percent of traffic allocation.
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
