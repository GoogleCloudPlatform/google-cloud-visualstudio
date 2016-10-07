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
using GoogleCloudExtension.SplitTrafficManagement;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;

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

        private bool _resourcesLoaded = false;

        private bool _showOnlyFlexVersions = true;
        private bool _showOnlyDotNetRuntimes = false;
        private bool _showOnlyVersionsWithTraffic = false;

        private List<VersionViewModel> _versions;

        public readonly GaeSourceRootViewModel root;

        public Service service { get; private set; }

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
                _showOnlyDotNetRuntimes = false;
                UpdateContextMenu();
                PresentViewModels();
            }
        }

        public bool ShowOnlyDotNetRuntimes
        {
            get { return _showOnlyDotNetRuntimes; }
            set
            {
                if (value == _showOnlyDotNetRuntimes)
                {
                    return;
                }
                _showOnlyDotNetRuntimes = value;
                _showOnlyFlexVersions = false;
                UpdateContextMenu();
                PresentViewModels();
            }
        }

        public bool ShowOnlyVersionsWithTraffic
        {
            get { return _showOnlyVersionsWithTraffic; }
            set
            {
                if (value == _showOnlyVersionsWithTraffic)
                {
                    return;
                }
                _showOnlyVersionsWithTraffic = value;
                UpdateContextMenu();
                PresentViewModels();
            }
        }

        public ServiceViewModel(GaeSourceRootViewModel owner, Service service)
        {
            _owner = owner;
            this.service = service;
            root = _owner;

            Initialize();
        }

        private void Initialize()
        {
            Caption = service.Id;
            Icon = s_serviceIcon.Value;

            Children.Add(s_loadingPlaceholder);

            UpdateContextMenu();
        }

        /// <summary>
        /// Update the context menu based on the current state of the service.
        /// </summary>
        private void UpdateContextMenu()
        {
            // Do not allow actions when the service is loading or in an error state.
            if (IsLoading || IsError)
            {
                ContextMenu = null;
                return;
            }

            var menuItems = new List<FrameworkElement>
            {
                new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new WeakCommand(OnOpenOnCloudConsoleCommand) },
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new WeakCommand(OnPropertiesWindowCommand) },
                new MenuItem { Header = Resources.CloudExplorerGaeServiceOpen, Command = new WeakCommand(OnOpenService) },
                
            };

            if (Children.Count > 1)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeSplitTraffic, Command = new WeakCommand(OnSplitTraffic) });
            }

            menuItems.Add(new Separator());

            if (!GaeUtils.AppEngineDefaultServiceName.Equals(service.Id))
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeDeleteService, Command = new WeakCommand(OnDeleteService) });
            }

            if (ShowOnlyFlexVersions)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowFlexAndStandardVersions, Command = new WeakCommand(OnShowFlexibleAndStandardVersions) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowFlexVersions, Command = new WeakCommand(OnShowOnlyFlexVersions) });
            }

            if (ShowOnlyDotNetRuntimes)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowAllRuntimes, Command = new WeakCommand(OnShowAllRuntimes) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowDotNetRuntimes, Command = new WeakCommand(OnShowOnlyDotNetRuntimes) });
            }

            if (ShowOnlyVersionsWithTraffic)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowWithAndWithoutTraffic, Command = new WeakCommand(OnShowVersionsWithAndWithoutTraffic) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowVersionsWithTraffic, Command = new WeakCommand(OnShowOnlyVersionsWithTraffic) });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }


        /// <summary>
        /// Present the view model based on the versions and filters.
        /// </summary>
        private void PresentViewModels()
        {
            if (_versions == null)
            {
                return;
            }

            IEnumerable<VersionViewModel> versions = _versions;
            if (ShowOnlyFlexVersions)
            {
                versions = versions.Where(x => x.version.Vm ?? false);
            }
            if (ShowOnlyDotNetRuntimes)
            {
                versions = versions.Where(
                    x => x.version?.Runtime.Equals(GaeVersionExtensions.DotNetRuntime) ?? false);
            }
            if (ShowOnlyVersionsWithTraffic)
            {
                versions = versions.Where(x => x.TrafficAllocation != null);
            }

            UpdateViewModels(versions);
        }

        /// <summary>
        /// Update the view model with the version models for display.
        /// </summary>
        private void UpdateViewModels(IEnumerable<VersionViewModel> versions)
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

        /// <summary>
        /// Opens the dialog to manage traffic splitting for the GAE service and 
        /// updates the traffic split if the user makes a change.
        /// </summary>
        private void OnSplitTraffic()
        {
            SplitTrafficChange change = SplitTrafficWindow.PromptUser(service, _versions.Select(x => x.version));
            if (change == null)
            {
                return;
            }

            TrafficSplit split = new TrafficSplit()
            {
                ShardBy = change.ShardBy,
                Allocations = change.Allocations,
            };
            UpdateTrafficSplit(split);
        }

        /// <summary>
        /// Promptes the user if they would like to delete this service.
        /// </summary>
        private void OnDeleteService()
        {
            string confirmationMessage = String.Format(
               Resources.CloudExplorerGaeDeleteServiceConfirmationPromptMessage, service.Id);
            if (!UserPromptUtils.YesNoPrompt(confirmationMessage, Resources.CloudExplorerGaeDeleteService))
            {
                Debug.WriteLine("The user cancelled deleting the service.");
                return;
            }

            DeleteService();
        }

        private void OnShowOnlyFlexVersions()
        {
            ShowOnlyFlexVersions = true;
        }

        private void OnShowFlexibleAndStandardVersions()
        {
            ShowOnlyFlexVersions = false;
        }

        private void OnShowOnlyDotNetRuntimes()
        {
            ShowOnlyDotNetRuntimes = true;
        }

        private void OnShowAllRuntimes()
        {
            ShowOnlyDotNetRuntimes = false;
        }

        private void OnShowOnlyVersionsWithTraffic()
        {
            ShowOnlyVersionsWithTraffic = true;
        }

        private void OnShowVersionsWithAndWithoutTraffic()
        {
            ShowOnlyVersionsWithTraffic = false;
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
                        UpdateContextMenu();
                    }
                }

                EventsReporterWrapper.ReportEvent(GaeVersionsLoadedEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine(Resources.CloudExplorerGaeFailedVersionsMessage);
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                EventsReporterWrapper.ReportEvent(GaeVersionsLoadedEvent.Create(CommandStatus.Failure));
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
        /// Update a service's traffic split.
        /// </summary>
        private async void UpdateTrafficSplit(TrafficSplit split)
        {
            IsLoading = true;
            // Ensure there are no children and they are not expanded so a
            // reload of children will occur after the split update happens.
            IsExpanded = false;
            Children.Clear();
            UpdateContextMenu();
            Caption = Resources.CloudExplorerGaeUpdateTrafficSplitMessage;
            GaeDataSource datasource = root.DataSource.Value;

            try
            {
                Task<Operation> operationTask = root.DataSource.Value.UpdateServiceTrafficSplit(split, service.Id);
                Func<Operation, Task<Operation>> fetch = (o) => datasource.GetOperationAsync(o.GetOperationId());
                Predicate<Operation> stopPolling = (o) => o.Done ?? false;
                Operation operation = await Polling<Operation>.Poll(await operationTask, fetch, stopPolling);
                if (operation.Error != null)
                {
                    throw new DataSourceException(operation.Error.Message);
                }
                service = await datasource.GetServiceAsync(service.Id);
                EventsReporterWrapper.ReportEvent(GaeTrafficSplitUpdatedEvent.Create(CommandStatus.Success));
            }
            catch (Exception ex)
            {
                EventsReporterWrapper.ReportEvent(GaeTrafficSplitUpdatedEvent.Create(CommandStatus.Failure));
                IsError = true;
                var message = "";

                if (ex is DataSourceException)
                {
                    message = ex.Message;
                }
                else if (ex is TimeoutException)
                {
                    message = Resources.CloudExploreOperationTimeoutMessage;
                }
                else if (ex is OperationCanceledException)
                {
                    message = Resources.CloudExploreOperationCanceledMessage;
                }
                else
                {
                    throw;
                }

                UserPromptUtils.ErrorPrompt(
                   message, Resources.CloudExplorerGaeDeleteServiceErrorMessage);
            }
            finally
            {
                IsLoading = false;
                Initialize();
            }
        }

        /// <summary>
        /// Deletes 'this' service.
        /// </summary>
        private async void DeleteService()
        {
            IsLoading = true;
            Children.Clear();
            UpdateContextMenu();
            Caption = String.Format(Resources.CloudExplorerGaeServiceDeleteMessage, service.Id);
            GaeDataSource datasource = root.DataSource.Value;

            try
            {
                Task<Operation> operationTask = root.DataSource.Value.DeleteServiceAsync(service.Id);
                Func<Operation, Task<Operation>> fetch = (o) => datasource.GetOperationAsync(o.GetOperationId());
                Predicate<Operation> stopPolling = (o) => o.Done ?? false;
                Operation operation = await Polling<Operation>.Poll(await operationTask, fetch, stopPolling);
                if (operation.Error != null)
                {
                    throw new DataSourceException(operation.Error.Message);
                }
                EventsReporterWrapper.ReportEvent(GaeServiceDeletedEvent.Create(CommandStatus.Success));
            }
            catch (Exception ex)
            {
                EventsReporterWrapper.ReportEvent(GaeServiceDeletedEvent.Create(CommandStatus.Failure));
                IsError = true;
                var message = "";

                if (ex is DataSourceException)
                {
                    message = ex.Message;
                }
                else if (ex is TimeoutException)
                {
                    message = Resources.CloudExploreOperationTimeoutMessage;
                }
                else if (ex is OperationCanceledException)
                {
                    message = Resources.CloudExploreOperationCanceledMessage;
                }
                else
                {
                    throw;
                }

                UserPromptUtils.ErrorPrompt(
                   message, Resources.CloudExplorerGaeDeleteServiceErrorMessage);
            }
            finally
            {
                IsLoading = false;

                if (IsError)
                {
                    Caption = service.Id;
                }
                else
                {
                    // Remove the deleted child.
                    _owner.Children.Remove(this);
                }
            }
        }

        /// <summary>
        /// Load a list of Flex versions sorted by percent of traffic allocation.
        /// </summary>
        private async Task<List<VersionViewModel>> LoadVersionList()
        {
            var versions = await root.DataSource.Value.GetVersionListAsync(service.Id);
            return versions?
                .Select(x => new VersionViewModel(this, x))
                .OrderByDescending(x => x.TrafficAllocation)
                .ToList();
        }

        public ServiceItem GetItem() => new ServiceItem(service);
    }
}
