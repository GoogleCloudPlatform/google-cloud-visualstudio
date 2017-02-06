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
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
        private const string IconStopedResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_stoped.png";
        private const string IconTransitionResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_transition.png";

        private static readonly Lazy<ImageSource> s_versionRunningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconRunningResourcePath));
        private static readonly Lazy<ImageSource> s_versionStopedIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconStopedResourcePath));
        private static readonly Lazy<ImageSource> s_versionTransitionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconTransitionResourcePath));

        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeNoInstancesFoundCaption,
            IsWarning = true
        };

        private readonly GaeSourceRootViewModel _owner;
        private readonly Service _service;
        private readonly Google.Apis.Appengine.v1.Data.Version _version;
        private readonly IList<InstanceViewModel> _instances;
        private readonly double _trafficAllocation;
        private readonly bool _hasTrafficAllocation;

        public Google.Apis.Appengine.v1.Data.Version Version => _version;

        public bool HasTrafficAllocation => _hasTrafficAllocation;

        public event EventHandler ItemChanged;

        public object Item => new VersionItem(_version);

        public VersionViewModel(
            GaeSourceRootViewModel owner,
            Service service,
            Google.Apis.Appengine.v1.Data.Version version,
            IList<InstanceViewModel> instances)
        {
            _owner = owner;
            _service = service;
            _version = version;
            _instances = instances;

            var allocation = GaeServiceExtensions.GetTrafficAllocation(_service, _version.Id);
            _trafficAllocation = allocation ?? 0.0;
            _hasTrafficAllocation = allocation != null;

            // Add the instances.
            foreach (var instance in _instances)
            {
                Children.Add(instance);
            }
            if (Children.Count == 0)
            {
                Children.Add(s_noItemsPlacehoder);
            }

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

            // If the version has traffic allocated to it it can be opened.
            if (_hasTrafficAllocation)
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeVersionOpen, Command = new ProtectedCommand(OnOpenVersion) });
            }

            menuItems.Add(new Separator());

            if (_version.IsServing())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeStopVersion, Command = new ProtectedCommand(OnStopVersion) });
            }
            else if (_version.IsStopped())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeStartVersion, Command = new ProtectedCommand(OnStartVersion) });
            }

            // If the version is stopped and has no traffic allocated to it allow it to be deleted.
            if (!_hasTrafficAllocation && _version.IsStopped())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGaeDeleteVersion, Command = new ProtectedCommand(OnDeleteVersion) });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnStartVersion()
        {
            // UpdateServingStatus(GaeVersionExtensions.ServingStatus, Resources.CloudExplorerGaeVersionStartServingMessage);
        }

        private void OnStopVersion()
        {
            // UpdateServingStatus(GaeVersionExtensions.StoppedStatus, Resources.CloudExplorerGaeVersionStopServingMessage);
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
                Task<Operation> operationTask = dataSource.DeleteVersionAsync(_service.Id, _version.Id);
                Func<Operation, Task<Operation>> fetch = (o) => dataSource.GetOperationAsync(o.GetOperationId());
                Predicate<Operation> stopPolling = (o) => o.Done ?? false;
                Operation operation = await Polling<Operation>.Poll(await operationTask, fetch, stopPolling);
                if (operation.Error != null)
                {
                    throw new DataSourceException(operation.Error.Message);
                }
                EventsReporterWrapper.ReportEvent(GaeVersionDeletedEvent.Create(CommandStatus.Success));
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(GaeVersionDeletedEvent.Create(CommandStatus.Failure));
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
            finally
            {
                IsLoading = false;

                // Re-initialize the instance as it will have a new version.
                if (!IsError)
                {
                    // Remove the deleted child.
                    _owner.Children.Remove(this);
                }
                else
                {
                    Caption = GetCaption();
                }
            }
        }

        /// <summary>
        /// Update the serving status of the version.
        /// </summary>
        /// <param name="status">The serving status to update the version to.</param>
        /// <param name="statusMessage">The message to display while updating the status</param>
        /*
        private async void UpdateServingStatus(string status, string statusMessage)
        {
            IsLoading = true;
            Children.Clear();
            UpdateMenu();
            Caption = statusMessage;
            GaeDataSource dataSource = _owner.DataSource;

            try
            {
                Task<Operation> operationTask = dataSource.UpdateVersionServingStatus(status, _service.Id, _version.Id);
                Func<Operation, Task<Operation>> fetch = (o) => dataSource.GetOperationAsync(o.GetOperationId());
                Predicate<Operation> stopPolling = (o) => o.Done ?? false;
                Operation operation = await Polling<Operation>.Poll(await operationTask, fetch, stopPolling);
                if (operation.Error != null)
                {
                    throw new DataSourceException(operation.Error.Message);
                }

                _version = await dataSource.GetVersionAsync(_service.Id, _version.Id);
                EventsReporterWrapper.ReportEvent(
                    GaeVersionServingStatusUpdatedEvent.Create(CommandStatus.Success, statusMessage));
            }
            catch (Exception ex) when (ex is DataSourceException || ex is TimeoutException || ex is OperationCanceledException)
            {
                EventsReporterWrapper.ReportEvent(
                    GaeVersionServingStatusUpdatedEvent.Create(CommandStatus.Failure, statusMessage));
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
            finally
            {
                IsLoading = false;

                // Re-initialize the instance as it will have a new version.
                if (!IsError)
                {
                    Initialize();
                }
                else
                {
                    Caption = GetCaption();
                }
            }
        }
        */

        /// <summary>
        /// Get a caption for a the version.
        /// Formated as 'versionId (traffic%)' if a traffic allocation is present, 'versionId' otherwise.
        /// </summary>
        private string GetCaption()
        {
            if (!_hasTrafficAllocation)
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
