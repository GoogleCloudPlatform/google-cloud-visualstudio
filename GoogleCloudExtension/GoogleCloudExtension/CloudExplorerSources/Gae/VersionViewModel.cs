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
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE version in the Google Cloud Explorer Window.
    /// </summary>
    internal class VersionViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconRunningResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_running.png";
        private const string IconStopedResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_stoped.png";
        private const string IconTransitionResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_transition.png";

        private static readonly Lazy<ImageSource> s_versionRunningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconRunningResourcePath));
        private static readonly Lazy<ImageSource> s_versionStopedIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconStopedResourcePath));
        private static readonly Lazy<ImageSource> s_versionTransitionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconTransitionResourcePath));

        private readonly GaeSourceRootViewModel _owner;
        private readonly Service _service;
        private readonly Google.Apis.Appengine.v1.Data.Version _version;
        private readonly double _trafficAllocation;
        private readonly bool _isLastVersion;

        public Google.Apis.Appengine.v1.Data.Version Version => _version;

        public bool HasTrafficAllocation => _trafficAllocation > 0;

        /// <summary>
        /// Determines if the current version can be deleted. A version can be deleted if:
        /// * Is not the last version in the service, the last version cannot be deleted, the whole
        ///   service has to be deleted instead.
        /// * If it is not the last version then it must not have traffic allocated to it. The user must move
        ///   the traffic away from the version before deleting it.
        /// </summary>
        private bool CanDeleteVersion => !_isLastVersion && !HasTrafficAllocation;

        public event EventHandler ItemChanged;

        public object Item => new VersionItem(_version);

        public VersionViewModel(
            GaeSourceRootViewModel owner,
            Service service,
            Google.Apis.Appengine.v1.Data.Version version,
            bool isLastVersion)
        {
            _owner = owner;
            _service = service;
            _version = version;
            _trafficAllocation = GaeServiceExtensions.GetTrafficAllocation(_service, _version.Id);
            _isLastVersion = isLastVersion;

            // Update the view.
            Caption = GetCaption();
            UpdateIcon();
            UpdateMenu();
        }

        /// <summary>
        /// Update the context menu based on the current state of the version.
        /// </summary>
        private void UpdateMenu()
        {
            var menuItems = new List<FrameworkElement>
            {
                new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new ProtectedCommand(OnOpenOnCloudConsoleCommand) },
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new ProtectedCommand(OnPropertiesWindowCommand) },
            };

            // If the version is running it can be opened.
            if (_version.IsServing())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeVersionOpen, Command = new ProtectedCommand(OnOpenVersion) });
                if (_trafficAllocation < 1.0)
                {
                    menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeMigrateAllTrafficHeader, Command = new ProtectedCommand(OnMigrateTrafficCommand) });
                }
            }

            menuItems.Add(new MenuItem { Header = Resources.CloudExplorerLaunchLogsViewerMenuHeader, Command = new ProtectedCommand(OnBrowseStackdriverLogCommand) });
            menuItems.Add(new Separator());

            if (_version.IsServing())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeStopVersion, Command = new ProtectedCommand(OnStopVersion) });
            }
            else if (_version.IsStopped())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeStartVersion, Command = new ProtectedCommand(OnStartVersion) });
            }

            if (CanDeleteVersion)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeDeleteVersion, Command = new ProtectedCommand(OnDeleteVersion) });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };

            SyncContextMenuState();
        }

        private void OnBrowseStackdriverLogCommand()
        {
            var window = ToolWindowCommandUtils.ShowToolWindow<LogsViewerToolWindow>();
            window?.FilterGAEServiceLog(_service.Id, _version.Id);
        }

        private async void OnMigrateTrafficCommand()
        {
            try
            {
                IsLoading = true;
                Caption = String.Format(Resources.CloudExplorerGaeMigratingAllTrafficCaption, _version.Id);

                var split = new TrafficSplit { Allocations = new Dictionary<string, double?> {[_version.Id] = 1.0 } };
                var operation = await _owner.DataSource.UpdateServiceTrafficSplitAsync(split, _service.Id);
                await _owner.DataSource.AwaitOperationAsync(operation);
                _owner.InvalidateService(_service.Id);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to set traffic to 100%: {ex.Message}");
                IsError = true;
                Caption = String.Format(Resources.CloudExplorerGaeFailedToMigrateAllTrafficCaption, _version.Id);
            }
        }

        private void OnStartVersion()
        {
            UpdateServingStatus(GaeVersionExtensions.ServingStatus, Resources.CloudExplorerGaeVersionStartServingMessage);
        }

        private void OnStopVersion()
        {
            UpdateServingStatus(GaeVersionExtensions.StoppedStatus, Resources.CloudExplorerGaeVersionStopServingMessage);
        }

        private void OnDeleteVersion()
        {
            string confirmationMessage = String.Format(
                Resources.CloudExplorerGaeDeleteVersionConfirmationPromptMessage, _service.Id, _version.Id);
            if (!UserPromptUtils.ActionPrompt(
                confirmationMessage,
                Resources.CloudExplorerGaeDeleteVersion,
                actionCaption: Resources.UiYesButtonCaption,
                cancelCaption: Resources.UiNoButtonCaption))
            {
                Debug.WriteLine("The user cancelled deleting the version.");
                return;
            }

            DeleteVersion();
        }

        private void OnOpenOnCloudConsoleCommand()
        {
            var url = $"https://console.cloud.google.com/appengine/instances?project={_owner.Context.CurrentProject.ProjectId}&moduleId={_service.Id}&versionId={_version.Id}";
            Process.Start(url);
        }

        private void OnPropertiesWindowCommand()
        {
            _owner.Context.ShowPropertiesWindow(Item);
        }

        private void OnOpenVersion()
        {
            Process.Start(_version.VersionUrl);
        }

        private async void DeleteVersion()
        {
            IsLoading = true;
            Children.Clear();
            UpdateMenu();
            Caption = Resources.CloudExplorerGaeVersionDeleteMessage;
            GaeDataSource dataSource = _owner.DataSource;

            try
            {
                var operation = await dataSource.DeleteVersionAsync(_service.Id, _version.Id);
                await dataSource.AwaitOperationAsync(operation);
                _owner.InvalidateService(_service.Id);

                EventsReporterWrapper.ReportEvent(GaeVersionDeletedEvent.Create(CommandStatus.Success));
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(GaeVersionDeletedEvent.Create(CommandStatus.Failure));
                IsLoading = false;
                IsError = true;

                if (ex is DataSourceException)
                {
                    Caption = Resources.CloudExplorerGaeDeleteVersionErrorMessage;
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
        }

        /// <summary>
        /// Update the serving status of the version.
        /// </summary>
        /// <param name="status">The serving status to update the version to.</param>
        /// <param name="statusMessage">The message to display while updating the status</param>
        private async void UpdateServingStatus(string status, string statusMessage)
        {
            IsLoading = true;
            Children.Clear();
            UpdateMenu();
            Caption = statusMessage;
            GaeDataSource dataSource = _owner.DataSource;

            try
            {
                var operation = await dataSource.UpdateVersionServingStatus(status, _service.Id, _version.Id);
                await dataSource.AwaitOperationAsync(operation);

                EventsReporterWrapper.ReportEvent(
                    GaeVersionServingStatusUpdatedEvent.Create(CommandStatus.Success, statusMessage));
                _owner.InvalidateService(_service.Id);
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(
                    GaeVersionServingStatusUpdatedEvent.Create(CommandStatus.Failure, statusMessage));
                IsLoading = false;
                IsError = true;

                if (ex is DataSourceException)
                {
                    Caption = Resources.CloudExplorerGaeUpdateServingStatusErrorMessage;
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
        }

        /// <summary>
        /// Get a caption for a the version.
        /// Formated as 'versionId (traffic%)' if a traffic allocation is present, 'versionId' otherwise.
        /// </summary>
        private string GetCaption()
        {
            if (!HasTrafficAllocation)
            {
                return _version.Id;
            }
            string percent = _trafficAllocation.ToString("P", CultureInfo.InvariantCulture);
            return String.Format("{0} ({1})", _version.Id, percent);
        }

        private void UpdateIcon()
        {
            switch (_version.ServingStatus)
            {
                case GaeVersionExtensions.ServingStatus:
                    Icon = s_versionRunningIcon.Value;
                    break;
                case GaeVersionExtensions.StoppedStatus:
                    Icon = s_versionStopedIcon.Value;
                    break;
                default:
                    Icon = s_versionTransitionIcon.Value;
                    break;
            }
        }
    }
}
