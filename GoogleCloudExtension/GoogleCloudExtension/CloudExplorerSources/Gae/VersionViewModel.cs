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
using GoogleCloudExtension.Services;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
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
        private const string IconStoppedResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_stopped.png";
        private const string IconTransitionResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_transition.png";

        private static readonly Lazy<ImageSource> s_versionRunningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconRunningResourcePath));
        private static readonly Lazy<ImageSource> s_versionStoppedIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconStoppedResourcePath));
        private static readonly Lazy<ImageSource> s_versionTransitionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconTransitionResourcePath));

        private readonly GaeSourceRootViewModel _owner;
        private readonly Service _service;
        private readonly double _trafficAllocation;
        private readonly bool _isLastVersion;

        public Google.Apis.Appengine.v1.Data.Version Version { get; }

        public bool HasTrafficAllocation => _trafficAllocation > 0;

        /// <summary>
        /// Determines if the current version can be deleted. A version can be deleted if:
        /// * Is not the last version in the service, the last version cannot be deleted, the whole
        ///   service has to be deleted instead.
        /// * If it is not the last version then it must not have traffic allocated to it. The user must move
        ///   the traffic away from the version before deleting it.
        /// </summary>
        private bool CanDeleteVersion => !_isLastVersion && !HasTrafficAllocation;

        #region ICloudExplorerItemSource implementation

        event EventHandler ICloudExplorerItemSource.ItemChanged
        {
            add { }
            remove { }
        }

        object ICloudExplorerItemSource.Item => GetItem();

        #endregion

        public VersionViewModel(
            GaeSourceRootViewModel owner,
            Service service,
            Google.Apis.Appengine.v1.Data.Version version,
            bool isLastVersion)
        {
            _owner = owner;
            _service = service;
            Version = version;
            _trafficAllocation = _service.GetTrafficAllocation(Version.Id);
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
                new MenuItem
                {
                    Header = Resources.UiOpenOnCloudConsoleMenuHeader,
                    Command = new ProtectedCommand(OnOpenOnCloudConsoleCommand)
                },
                new MenuItem
                {
                    Header = Resources.UiPropertiesMenuHeader,
                    Command = new ProtectedAsyncCommand(OnPropertiesWindowCommandAsync)
                }
            };

            // If the version is running it can be opened.
            if (Version.IsServing())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeVersionOpen, Command = new ProtectedCommand(OnOpenVersion) });
                if (_trafficAllocation < 1.0)
                {
                    menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeMigrateAllTrafficHeader, Command = new ProtectedAsyncCommand(OnMigrateTrafficCommandAsync) });
                }
            }

            menuItems.Add(
                new MenuItem
                {
                    Header = Resources.CloudExplorerLaunchLogsViewerMenuHeader,
                    Command = new ProtectedAsyncCommand(OnBrowseStackdriverLogCommandAsync)
                });
            menuItems.Add(new Separator());

            if (Version.IsServing())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeStopVersion, Command = new ProtectedAsyncCommand(OnStopVersionAsync) });
            }
            else if (Version.IsStopped())
            {
                menuItems.Add(new MenuItem
                {
                    Header = Resources.CloudExplorerGaeStartVersion,
                    Command = new
                    ProtectedAsyncCommand(OnStartVersionAsync)
                });
            }

            if (CanDeleteVersion)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeDeleteVersion, Command = new ProtectedAsyncCommand(OnDeleteVersionAsync) });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };

            SyncContextMenuState();
        }

        private async Task OnBrowseStackdriverLogCommandAsync()
        {
            LogsViewerToolWindow window = await ToolWindowCommandUtils.AddToolWindowAsync<LogsViewerToolWindow>();
            window?.FilterGAEServiceLog(_service.Id, Version.Id);
        }

        private async Task OnMigrateTrafficCommandAsync()
        {
            try
            {
                IsLoading = true;
                Caption = string.Format(Resources.CloudExplorerGaeMigratingAllTrafficCaption, Version.Id);

                var split = new TrafficSplit { Allocations = new Dictionary<string, double?> { [Version.Id] = 1.0 } };
                await _owner.DataSource.UpdateServiceTrafficSplitAsync(split, _service.Id);
                await _owner.InvalidateServiceAsync(_service.Id);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"Failed to set traffic to 100%: {ex.Message}");
                IsError = true;
                Caption = string.Format(Resources.CloudExplorerGaeFailedToMigrateAllTrafficCaption, Version.Id);
            }
        }

        private async Task OnStartVersionAsync() => await UpdateServingStatusAsync(GaeVersionExtensions.ServingStatus, Resources.CloudExplorerGaeVersionStartServingMessage);

        private async Task OnStopVersionAsync() => await UpdateServingStatusAsync(GaeVersionExtensions.StoppedStatus, Resources.CloudExplorerGaeVersionStopServingMessage);

        private async Task OnDeleteVersionAsync()
        {
            string confirmationMessage = string.Format(
                Resources.CloudExplorerGaeDeleteVersionConfirmationPromptMessage, _service.Id, Version.Id);
            if (!UserPromptService.Default.ActionPrompt(
                confirmationMessage,
                Resources.CloudExplorerGaeDeleteVersion,
                actionCaption: Resources.UiYesButtonCaption,
                cancelCaption: Resources.UiNoButtonCaption))
            {
                Debug.WriteLine("The user cancelled deleting the version.");
                return;
            }

            await DeleteVersionAsync();
        }

        private void OnOpenOnCloudConsoleCommand()
        {
            string url = "https://console.cloud.google.com/appengine/instances" +
                $"?project={_owner.Context.CurrentProject.ProjectId}" +
                $"&moduleId={_service.Id}" +
                $"&versionId={Version.Id}";
            Process.Start(url);
        }

        private async Task OnPropertiesWindowCommandAsync() => await _owner.Context.ShowPropertiesWindowAsync(GetItem());

        private void OnOpenVersion() => Process.Start(Version.VersionUrl);

        private async Task DeleteVersionAsync()
        {
            IsLoading = true;
            Children.Clear();
            UpdateMenu();
            Caption = Resources.CloudExplorerGaeVersionDeleteMessage;
            GaeDataSource dataSource = _owner.DataSource;

            try
            {
                await dataSource.DeleteVersionAsync(_service.Id, Version.Id);
                await _owner.InvalidateServiceAsync(_service.Id);

                EventsReporterWrapper.ReportEvent(GaeVersionDeletedEvent.Create(CommandStatus.Success));
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(GaeVersionDeletedEvent.Create(CommandStatus.Failure));
                IsLoading = false;
                IsError = true;

                switch (ex)
                {
                    case DataSourceException _:
                        Caption = Resources.CloudExplorerGaeDeleteVersionErrorMessage;
                        break;
                    case TimeoutException _:
                        Caption = Resources.CloudExploreOperationTimeoutMessage;
                        break;
                    case OperationCanceledException _:
                        Caption = Resources.CloudExploreOperationCanceledMessage;
                        break;
                }
            }
        }

        /// <summary>
        /// Update the serving status of the version.
        /// </summary>
        /// <param name="status">The serving status to update the version to.</param>
        /// <param name="statusMessage">The message to display while updating the status</param>
        private async Task UpdateServingStatusAsync(string status, string statusMessage)
        {
            IsLoading = true;
            Children.Clear();
            UpdateMenu();
            Caption = statusMessage;
            GaeDataSource dataSource = _owner.DataSource;

            try
            {
                await dataSource.UpdateVersionServingStatus(status, _service.Id, Version.Id);

                EventsReporterWrapper.ReportEvent(
                    GaeVersionServingStatusUpdatedEvent.Create(CommandStatus.Success, statusMessage));
                await _owner.InvalidateServiceAsync(_service.Id);
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(
                    GaeVersionServingStatusUpdatedEvent.Create(CommandStatus.Failure, statusMessage));
                IsLoading = false;
                IsError = true;

                switch (ex)
                {
                    case DataSourceException _:
                        Caption = Resources.CloudExplorerGaeUpdateServingStatusErrorMessage;
                        break;
                    case TimeoutException _:
                        Caption = Resources.CloudExploreOperationTimeoutMessage;
                        break;
                    case OperationCanceledException _:
                        Caption = Resources.CloudExploreOperationCanceledMessage;
                        break;
                }
            }
        }

        /// <summary>
        /// Get a caption for a the version.
        /// Formatted as 'versionId (traffic%)' if a traffic allocation is present, 'versionId' otherwise.
        /// </summary>
        private string GetCaption()
        {
            if (!HasTrafficAllocation)
            {
                return Version.Id;
            }
            string percent = _trafficAllocation.ToString("P", CultureInfo.InvariantCulture);
            return $"{Version.Id} ({percent})";
        }

        private void UpdateIcon()
        {
            switch (Version.ServingStatus)
            {
                case GaeVersionExtensions.ServingStatus:
                    Icon = s_versionRunningIcon.Value;
                    break;
                case GaeVersionExtensions.StoppedStatus:
                    Icon = s_versionStoppedIcon.Value;
                    break;
                default:
                    Icon = s_versionTransitionIcon.Value;
                    break;
            }
        }

        private VersionItem GetItem() => new VersionItem(Version);
    }
}
