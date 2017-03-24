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

using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.SplitTrafficManagement;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE service in the Google Cloud Explorer Window.
    /// </summary>
    internal class ServiceViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconServiceResourcePath = "CloudExplorerSources/Gae/Resources/service_icon.png";

        private static readonly Lazy<ImageSource> s_serviceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconServiceResourcePath));

        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeNoVersionsFoundCaption,
            IsWarning = true
        };

        private readonly GaeSourceRootViewModel _owner;
        private readonly Service _service;
        private readonly IList<VersionViewModel> _versions;

        private bool _showOnlyFlexVersions = false;
        private bool _showOnlyDotNetRuntimes = false;
        private bool _showOnlyVersionsWithTraffic = false;

        public Service Service => _service;

        public event EventHandler ItemChanged;

        public object Item => GetItem();

        public bool ShowOnlyFlexVersions
        {
            get { return _showOnlyFlexVersions; }
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

        public ServiceViewModel(GaeSourceRootViewModel owner, Service service, IList<VersionViewModel> versions)
        {
            _owner = owner;
            _versions = versions;
            _service = service;

            Caption = Service.Id;
            Icon = s_serviceIcon.Value;

            UpdateContextMenu();
            PresentViewModels();
        }

        /// <summary>
        /// Update the context menu based on the current state of the service.
        /// </summary>
        private void UpdateContextMenu()
        {
            var menuItems = new List<FrameworkElement>
            {
                new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new ProtectedCommand(OnOpenOnCloudConsoleCommand) },
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new ProtectedCommand(OnPropertiesWindowCommand) },
                new MenuItem { Header = Resources.CloudExplorerGaeServiceOpen, Command = new ProtectedCommand(OnOpenService) },
            };

            menuItems.Add(new MenuItem
            {
                Header = Resources.CloudExplorerGaeSplitTraffic,
                Command = new ProtectedCommand(OnSplitTraffic, canExecuteCommand: _versions.Count > 1)
            });

            menuItems.Add(new MenuItem { Header = Resources.CloudExplorerLaunchLogsViewerMenuHeader, Command = new ProtectedCommand(OnBrowseStackdriverLogCommand) });
            menuItems.Add(new Separator());

            menuItems.Add(new MenuItem
            {
                Header = Resources.CloudExplorerGaeDeleteService,
                Command = new ProtectedCommand(OnDeleteService, canExecuteCommand: Service.Id != GaeUtils.AppEngineDefaultServiceName)
            });


            if (ShowOnlyFlexVersions)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowFlexAndStandardVersions, Command = new ProtectedCommand(OnShowFlexibleAndStandardVersions) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowFlexVersions, Command = new ProtectedCommand(OnShowOnlyFlexVersions) });
            }

            if (ShowOnlyDotNetRuntimes)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowAllRuntimes, Command = new ProtectedCommand(OnShowAllRuntimes) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowDotNetRuntimes, Command = new ProtectedCommand(OnShowOnlyDotNetRuntimes) });
            }

            if (ShowOnlyVersionsWithTraffic)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowWithAndWithoutTraffic, Command = new ProtectedCommand(OnShowVersionsWithAndWithoutTraffic) });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeShowVersionsWithTraffic, Command = new ProtectedCommand(OnShowOnlyVersionsWithTraffic) });
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
                versions = versions.Where(x => x.Version.Env == GaeVersionExtensions.FlexibleEnvironment);
            }
            if (ShowOnlyDotNetRuntimes)
            {
                versions = versions.Where(x => x.Version.Runtime == GaeVersionExtensions.AspNetCoreRuntime);
            }
            if (ShowOnlyVersionsWithTraffic)
            {
                versions = versions.Where(x => x.HasTrafficAllocation);
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
            SplitTrafficChange change = SplitTrafficWindow.PromptUser(Service, _versions.Select(x => x.Version));
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
               Resources.CloudExplorerGaeDeleteServiceConfirmationPromptMessage, Service.Id);
            if (!UserPromptUtils.ActionPrompt(
                prompt: confirmationMessage,
                title: Resources.CloudExplorerGaeDeleteService,
                actionCaption: Resources.UiYesButtonCaption,
                cancelCaption: Resources.UiNoButtonCaption))
            {
                Debug.WriteLine("The user cancelled deleting the service.");
                return;
            }

            DeleteService();
        }

        private void OnBrowseStackdriverLogCommand()
        {
            var window = ToolWindowCommandUtils.ShowToolWindow<LogsViewerToolWindow>();
            window?.FilterGAEServiceLog(Service.Id);
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

        private void OnOpenOnCloudConsoleCommand()
        {
            var url = $"https://console.cloud.google.com/appengine/versions?project={_owner.Context.CurrentProject.ProjectId}&moduleId={Service.Id}";
            Process.Start(url);
        }

        private void OnPropertiesWindowCommand()
        {
            _owner.Context.ShowPropertiesWindow(Item);
        }

        private async void OnOpenService()
        {
            var app = await _owner.GaeApplication;
            var url = GaeUtils.GetAppUrl(app.DefaultHostname, Service.Id);
            Process.Start(url);
        }

        /// <summary>
        /// Update a service's traffic split.
        /// </summary>
        private async void UpdateTrafficSplit(TrafficSplit split)
        {
            IsLoading = true;
            Children.Clear();
            UpdateContextMenu();
            Caption = Resources.CloudExplorerGaeUpdateTrafficSplitMessage;
            GaeDataSource datasource = _owner.DataSource;

            try
            {
                var operation = await _owner.DataSource.UpdateServiceTrafficSplitAsync(split, Service.Id);
                await _owner.DataSource.AwaitOperationAsync(operation);
                _owner.InvalidateService(_service.Id);

                EventsReporterWrapper.ReportEvent(GaeTrafficSplitUpdatedEvent.Create(CommandStatus.Success));
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(GaeTrafficSplitUpdatedEvent.Create(CommandStatus.Failure));
                IsError = true;

                if (ex is DataSourceException)
                {
                    Caption = Resources.CloudExplorerGaeUpdateTrafficSplitErrorMessage;
                }
                else if (ex is TimeoutException)
                {
                    Caption = Resources.CloudExploreOperationTimeoutMessage;
                }
                else if (ex is OperationCanceledException)
                {
                    Caption = Resources.CloudExploreOperationCanceledMessage;
                }
            }
            finally
            {
                IsLoading = false;
                PresentViewModels();
                Icon = s_serviceIcon.Value;
                UpdateContextMenu();
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
            Caption = String.Format(Resources.CloudExplorerGaeServiceDeleteMessage, Service.Id);
            GaeDataSource datasource = _owner.DataSource;

            try
            {
                var operation = await datasource.DeleteServiceAsync(Service.Id);
                await datasource.AwaitOperationAsync(operation);

                EventsReporterWrapper.ReportEvent(GaeServiceDeletedEvent.Create(CommandStatus.Success));
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(GaeServiceDeletedEvent.Create(CommandStatus.Failure));
                IsError = true;

                if (ex is DataSourceException)
                {
                    Caption = Resources.CloudExplorerGaeDeleteServiceErrorMessage;
                }
                else if (ex is TimeoutException)
                {
                    Caption = Resources.CloudExploreOperationTimeoutMessage;
                }
                else if (ex is OperationCanceledException)
                {
                    Caption = Resources.CloudExploreOperationCanceledMessage;
                }
            }
            finally
            {
                IsLoading = false;
                if (!IsError)
                {
                    // Remove the deleted child.
                    _owner.Children.Remove(this);
                }
            }
        }

        public ServiceItem GetItem() => new ServiceItem(Service);
    }
}
