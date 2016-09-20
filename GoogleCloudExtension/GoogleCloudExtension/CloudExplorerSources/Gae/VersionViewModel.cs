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

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE version in the Google Cloud Explorer Window.
    /// </summary>
    class VersionViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconRunningResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_running.png";
        private const string IconStopedResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_stoped.png";

        private static readonly Lazy<ImageSource> s_versionRunningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconRunningResourcePath));
        private static readonly Lazy<ImageSource> s_versionStopedIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconStopedResourcePath));

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeLoadingInstancesCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeNoInstancesFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeFailedToLoadInstancesCaption,
            IsError = true
        };

        private readonly ServiceViewModel _owner;

        public readonly GaeSourceRootViewModel root;

        private bool _resourcesLoaded;

        public readonly Google.Apis.Appengine.v1.Data.Version version;

        public event EventHandler ItemChanged;

        public object Item => GetItem();

        public VersionViewModel(
            ServiceViewModel owner, Google.Apis.Appengine.v1.Data.Version version)
        {
            _owner = owner;
            this.version = version;
            root = _owner.root;

            Caption = GetCaption();
            UpdateIcon();

            _resourcesLoaded = false;
            Children.Add(s_loadingPlaceholder);

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new WeakCommand(OnOpenOnCloudConsoleCommand) },
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new WeakCommand(OnPropertiesWindowCommand) },
            };

            // If the version has traffic allocated to it it can be opened.
            double? trafficAllocation = GaeServiceExtensions.GetTrafficAllocation(_owner.service, version.Id);
            if (trafficAllocation != null)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeVersionOpen, Command = new WeakCommand(OnOpenVersion) });
            }

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
                    var instances = await LoadInstanceList();
                    Children.Clear();
                    if (instances == null)
                    {
                        Children.Add(s_errorPlaceholder);
                    }
                    else
                    {
                        foreach (var item in instances)
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
                GcpOutputWindow.OutputLine(Resources.CloudExplorerGaeFailedInstancesMessage);
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private void OnOpenOnCloudConsoleCommand()
        {
            var url = $"https://console.cloud.google.com/appengine/instances?project={root.Context.CurrentProject.ProjectId}&moduleId={_owner.service.Id}&versionId={version.Id}";
            Process.Start(url);
        }

        private void OnPropertiesWindowCommand()
        {
            root.Context.ShowPropertiesWindow(Item);
        }

        private void OnOpenVersion()
        {
            Process.Start(version.VersionUrl);
        }

        /// <summary>
        /// Load a list of instances.
        /// </summary>
        private async Task<List<InstanceViewModel>> LoadInstanceList()
        {
            var instances = await _owner.root.DataSource.Value.GetInstanceListAsync(_owner.service.Id, version.Id);
            return instances?.Select(x => new InstanceViewModel(this, x)).ToList();
        }

        /// <summary>
        /// Get a caption for a the version.
        /// Formated as 'versionId (traffic%)' if a traffic allocation is present, 'versionId' otherwise.
        /// </summary>
        private string GetCaption()
        {
            double? trafficAllocation = GaeServiceExtensions.GetTrafficAllocation(_owner.service, version.Id);
            if (trafficAllocation == null)
            {
                return version.Id;
            }
            string percent = ((double)trafficAllocation).ToString("P", CultureInfo.InvariantCulture);
            return String.Format("{0} ({1})", version.Id, percent);
        }

        private void UpdateIcon()
        {
            double? trafficAllocation = GaeServiceExtensions.GetTrafficAllocation(_owner.service, version.Id);
            if (trafficAllocation != null)
            {
                Icon = s_versionRunningIcon.Value;
            }
            else
            {
                Icon = s_versionStopedIcon.Value;
            }
        }

        public VersionItem GetItem() => new VersionItem(version);
    }
}
