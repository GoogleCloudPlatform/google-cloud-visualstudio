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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
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

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceSourceRootViewModel : SourceRootViewModelBase
    {
        private const string ComputeApiName = "compute_component";

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGceSourceLoadingZonesCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGceSourceFailedLoadingZonesCaption,
            IsError = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGceSourceNoInstancesCaption,
            IsWarning = true
        };

        private bool _showOnlyWindowsInstances = false;
        private bool _showZones = false;
        private IList<InstancesPerZone> _instancesPerZone;
        private Lazy<GceDataSource> _dataSource;

        public GceDataSource DataSource => _dataSource.Value;

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override string RootCaption => Resources.CloudExplorerGceRootNodeCaption;

        /// <summary>
        /// Whether the list should be filter down to only those VMs that are running Windows.
        /// </summary>
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
                PresentViewModels();
                UpdateContextMenu();
                ShowOnlyWindowsInstancesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Whether the list should be shown as a tree of zones. If false then the list should be shown
        /// as a plain list of VMs.
        /// </summary>
        public bool ShowZones
        {
            get { return _showZones; }
            set
            {
                if (value == _showZones)
                {
                    return;
                }
                _showZones = value;
                PresentViewModels();
                UpdateContextMenu();
            }
        }

        /// <summary>
        /// This event is raised every time the <seealso cref="ShowOnlyWindowsInstances"/> property value changes.
        /// </summary>
        public event EventHandler ShowOnlyWindowsInstancesChanged;

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);

            InvalidateProjectOrAccount();
            UpdateContextMenu();
        }

        private void UpdateContextMenu()
        {
            var menuItems = new List<FrameworkElement>
            {
                new MenuItem { Header = Resources.CloudExplorerStatusMenuHeader, Command = new ProtectedCommand(OnStatusCommand) },
                new MenuItem { Header = Resources.CloudExplorerGceNewAspNetInstanceMenuHeader, Command = new ProtectedCommand(OnNewAspNetInstanceCommand) },
                new MenuItem { Header = Resources.CloudExplorerGceNewInstanceMenuHeader, Command = new ProtectedCommand(OnNewInstanceCommand) },
                new Separator(),
            };

            if (ShowOnlyWindowsInstances)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceShowAllOsInstancesCommand, Command = new ProtectedCommand(OnShowAllOsInstancesCommand) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceShowWindowsOnlyInstancesCommand, Command = new ProtectedCommand(OnShowOnlyWindowsInstancesCommand) });
            }

            if (ShowZones)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceShowInstancesCommand, Command = new ProtectedCommand(OnShowInstancesCommand) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceShowZonesCommand, Command = new ProtectedCommand(OnShowZonesCommand) });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnShowOnlyWindowsInstancesCommand()
        {
            ShowOnlyWindowsInstances = true;
        }

        private void OnShowAllOsInstancesCommand()
        {
            ShowOnlyWindowsInstances = false;
        }

        private void OnShowInstancesCommand()
        {
            ShowZones = false;
        }

        private void OnShowZonesCommand()
        {
            ShowZones = true;
        }

        private void OnNewAspNetInstanceCommand()
        {
            var url = $"https://console.cloud.google.com/launcher/details/click-to-deploy-images/aspnet?project={Context.CurrentProject.ProjectId}";
            Process.Start(url);
        }

        private void OnNewInstanceCommand()
        {
            var url = $"https://console.cloud.google.com/compute/instancesAdd?project={Context.CurrentProject.ProjectId}";
            Process.Start(url);
        }

        private void OnStatusCommand()
        {
            Process.Start("https://status.cloud.google.com/");
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
                return new GceDataSource(
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
                _instancesPerZone = null;
                _instancesPerZone = await _dataSource.Value.GetAllInstancesPerZonesAsync();
                PresentViewModels();

                EventsReporterWrapper.ReportEvent(GceVMsLoadedEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException ex)
            {
                var innerEx = ex.InnerException as GoogleApiException;
                if (innerEx != null && innerEx.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Debug.WriteLine("The GCE API is not enabled.");

                    // Show the node that notifies users that the API is disabled.
                    Children.Clear();
                    Children.Add(new DisabledApiWarning(
                        apiName: ComputeApiName,
                        caption: Resources.CloudExplorerGceSourceApiDisabledMessage,
                        project: Context.CurrentProject));
                    return;
                }

                EventsReporterWrapper.ReportEvent(GceVMsLoadedEvent.Create(CommandStatus.Failure));
                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private void PresentViewModels()
        {
            // If there's no data then there's nothing to do.
            if (_instancesPerZone == null)
            {
                return;
            }

            IEnumerable<TreeNode> viewModels;
            if (_showZones)
            {
                // This query creates the zone view model with the following steps:
                //   * Create an object that represents the zone and the instances that must be shown (after filtering)
                //     for that zone. The instances in the zone are sorted by their name.
                //   * Filter out the zones that (after filtering instances) are empty.
                //   * Sort the resulting zones by the zone name.
                //   * Create the view model for each zone, containing the instances for that zone.
                viewModels = _instancesPerZone
                    .Select(x => new
                    {
                        Zone = x.Zone,
                        Instances = x.Instances
                            .Where(i => _showOnlyWindowsInstances ? i.IsWindowsInstance() : true)
                            .OrderBy(i => i.Name)
                    })
                    .Where(x => x.Instances.Count() > 0)
                    .OrderBy(x => x.Zone.Name)
                    .Select(x => new ZoneViewModel(this, x.Zone, x.Instances.Select(i => new GceInstanceViewModel(this, i))));
            }
            else
            {
                // This query gets the list of view models for the instnaces with the following steps:
                //   * Select all of the instances in all of the zones in a source list.
                //   * Filters the instances according to the _showOnlyWindowsInstances setting.
                //   * Sorts the resulting instances by name.
                //   * Creates the view model for each instance.
                viewModels = _instancesPerZone
                    .SelectMany(x => x.Instances)
                    .Where(x => _showOnlyWindowsInstances ? x.IsWindowsInstance() : true)
                    .OrderBy(x => x.Name)
                    .Select(x => new GceInstanceViewModel(this, x));
            }
            UpdateViewModels(viewModels);
        }

        private void UpdateViewModels(IEnumerable<TreeNode> viewModels)
        {
            Children.Clear();
            foreach (var viewModel in viewModels)
            {
                Children.Add(viewModel);
            }
            if (Children.Count == 0)
            {
                Children.Add(s_noItemsPlacehoder);
            }
        }
    }
}