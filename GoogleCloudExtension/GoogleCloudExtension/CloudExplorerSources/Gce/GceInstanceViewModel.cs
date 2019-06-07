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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.AttachDebuggerDialog;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.TerminalServer;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.WindowsCredentialsChooser;
using Microsoft.Win32;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private static readonly TimeSpan s_pollTimeout = new TimeSpan(0, 0, 10);

        private const string IconRunningResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_running.png";
        private const string IconStopedResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_stoped.png";
        private const string IconTransitionResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_transition.png";

        private static readonly Lazy<ImageSource> s_instanceRunningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconRunningResourcePath));
        private static readonly Lazy<ImageSource> s_instanceStopedIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconStopedResourcePath));
        private static readonly Lazy<ImageSource> s_instanceTransitionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconTransitionResourcePath));

        private readonly GceSourceRootViewModel _owner;
        private Instance _instance;
        private ProtectedAsyncCommand _attachDebuggerCommand;

        private Instance Instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                UpdateIcon();
                ItemChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object Item
        {
            get
            {
                if (Instance.IsAppEngineFlexInstance())
                {
                    return new GceGaeInstanceItem(Instance);
                }
                else if (Instance.IsWindowsInstance())
                {
                    return new WindowsInstanceItem(Instance);
                }
                else
                {
                    return new GceInstanceItem(Instance);
                }
            }
        }

        public event EventHandler ItemChanged;

        public GceInstanceViewModel(GceSourceRootViewModel owner, Instance instance)
        {
            _owner = owner;
            Instance = instance;

            ErrorHandlerUtils.HandleExceptionsAsync(UpdateInstanceStateAsync);
        }

        public override void OnMenuItemOpen()
        {
            // In current code, _attachDebuggerCommand won't be null
            // To be safe and in case the constructor/initiailzation code could be modified in the future.
            if (_attachDebuggerCommand != null)
            {
                _attachDebuggerCommand.CanExecuteCommand =
                    Instance.IsWindowsInstance() && Instance.IsRunning() && !ShellUtils.Default.IsBusy();
            }
            base.OnMenuItemOpen();
        }

        /// <summary>
        /// Sync instance state when metadata of the instance changed outside.
        /// </summary>
        private async Task RefreshInstanceStateAsync()
        {
            IsLoading = true;
            try
            {
                Instance = await _owner.DataSource.RefreshInstance(Instance);
            }
            catch (DataSourceException ex)
            {
                Debug.WriteLine($"RefreshInstanceState failed {ex}");
                IsError = true; // Set state to error
            }
            finally
            {
                IsLoading = false;
            }
            Caption = Instance.Name;
            UpdateContextMenu();
        }

        private async Task UpdateInstanceStateAsync()
        {
            GceOperation pendingOperation = _owner.DataSource.GetPendingOperation(Instance);
            await UpdateInstanceStateAsync(pendingOperation);
        }

        private async Task UpdateInstanceStateAsync(GceOperation pendingOperation)
        {
            while (pendingOperation != null && !pendingOperation.OperationTask.IsCompleted)
            {
                // Since there's a pending operation the loading state needs to be set to show
                // progress ui.
                IsLoading = true;

                // Setting the content according to the operation type.
                switch (pendingOperation.OperationType)
                {
                    case OperationType.StartInstance:
                        Caption = String.Format(Resources.CloudExplorerGceInstanceStartingCaption, Instance.Name);
                        break;

                    case OperationType.StopInstance:
                        Caption = String.Format(Resources.CloudExplorerGceInstanceStoppingCaption, Instance.Name);
                        break;

                    case OperationType.SettingTags:
                        Caption = String.Format(Resources.CloudExplorerGceInstanceSettingTagsCaption, Instance.Name);
                        break;

                    case OperationType.ModifyingFirewall:
                        Caption = String.Format(Resources.CloudExplorerGceInstanceUpdatingFirewallCaption, Instance.Name);
                        break;
                }

                // Update the context menu to reflect the state.
                UpdateContextMenu();

                try
                {
                    await WhenOperationAsync(pendingOperation);

                    // Refresh the instance state after the operation is finished.
                    Instance = await _owner.DataSource.RefreshInstance(Instance);
                }
                catch (DataSourceException ex)
                {
                    Caption = Instance.Name;
                    IsLoading = false;
                    IsError = true;
                    UpdateContextMenu();

                    Debug.WriteLine("Previous operation failed.");
                    switch (pendingOperation.OperationType)
                    {
                        case OperationType.StartInstance:
                            await GcpOutputWindow.Default.OutputLineAsync(String.Format(Resources.CloudExplorerGceStartOperationFailedMessage, Instance.Name, ex.Message));
                            break;

                        case OperationType.StopInstance:
                            await GcpOutputWindow.Default.OutputLineAsync(String.Format(Resources.CloudExplorerGceStopOperationFailedMessage, Instance.Name, ex.Message));
                            break;
                    }

                    // Permanent error.
                    return;
                }

                // See if there are more operations.
                pendingOperation = _owner.DataSource.GetPendingOperation(Instance);
            }

            // Normal state, no pending operations.
            IsLoading = false;
            Caption = Instance.Name;
            UpdateContextMenu();
        }

        ///<summary>
        /// Awaits the end of the pending operation.
        /// We can also get here if the task is faulted, in which case we need to handle that case.
        /// </summary>
        private async Task WhenOperationAsync(GceOperation pendingOperation)
        {
            while (true)
            {
                // Refresh the instance before waiting for the operation to finish.
                Instance = await _owner.DataSource.RefreshInstance(Instance);

                // Wait for the operation to finish up to the timeout, which we will use to refresh the
                // state of the instance.
                var result = await Task.WhenAny(pendingOperation.OperationTask, Task.Delay(s_pollTimeout));
                if (result == pendingOperation.OperationTask)
                {
                    // Await the task again to get any possible exception.
                    await pendingOperation.OperationTask;
                    break;
                }
            }
        }

        private void UpdateContextMenu()
        {
            var getPublishSettingsCommand = new ProtectedCommand(OnSavePublishSettingsCommand, canExecuteCommand: Instance.IsAspnetInstance());
            var openWebSite = new ProtectedCommand(OnOpenWebsite, canExecuteCommand: Instance.IsAspnetInstance() && Instance.IsRunning());
            var openTerminalServerSessionCommand = new ProtectedCommand(
                OnOpenTerminalServerSessionCommand,
                canExecuteCommand: Instance.IsWindowsInstance() && Instance.IsRunning());
            var startInstanceCommand = new ProtectedAsyncCommand(OnStartInstanceCommandAsync);
            var stopInstanceCommand = new ProtectedAsyncCommand(OnStopInstanceCommandAsync);
            var manageFirewallPorts = new ProtectedAsyncCommand(OnManageFirewallPortsCommandAsync);
            var manageWindowsCredentials = new ProtectedCommand(OnManageWindowsCredentialsCommand, canExecuteCommand: Instance.IsWindowsInstance());
            _attachDebuggerCommand = new ProtectedAsyncCommand(OnAttachDebuggerAsync);

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.CloudExplorerGceSavePublishSettingsMenuHeader, Command = getPublishSettingsCommand},
                new MenuItem { Header = Resources.CloudExplorerGceOpenTerminalSessionMenuHeader, Command = openTerminalServerSessionCommand },
                new MenuItem { Header = Resources.CloudExplorerGceOpenWebSiteMenuHeader, Command = openWebSite },
                new MenuItem { Header = Resources.CloudExplorerGceManageFirewallPortsMenuHeader, Command = manageFirewallPorts },
                new MenuItem { Header = Resources.CloudExplorerGceManageWindowsCredentialsMenuHeader, Command = manageWindowsCredentials },
                new MenuItem { Header = Resources.CloudExplorerGceAttachDebuggerMenuHeader, Command = _attachDebuggerCommand }
            };

            if (Instance.Id.HasValue)
            {
                menuItems.Add(
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerLaunchLogsViewerMenuHeader,
                        Command = new ProtectedAsyncCommand(OnBrowseStackdriverLogCommandAsync)
                    });
            }

            if (Instance.IsRunning())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceStopInstanceMenuHeader, Command = stopInstanceCommand });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceStartInstanceMenuHeader, Command = startInstanceCommand });
            }

            menuItems.Add(
                new MenuItem
                {
                    Header = Resources.UiOpenOnCloudConsoleMenuHeader,
                    Command = new ProtectedCommand(OnOpenOnCloudConsoleCommand)
                });
            menuItems.Add(
                new MenuItem
                {
                    Header = Resources.UiPropertiesMenuHeader,
                    Command = new ProtectedAsyncCommand(OnPropertiesWindowCommandAsync)
                });

            ContextMenu = new ContextMenu { ItemsSource = menuItems };

            SyncContextMenuState();
        }

        private async Task OnBrowseStackdriverLogCommandAsync()
        {
            LogsViewerToolWindow window = await ToolWindowCommandUtils.AddToolWindowAsync<LogsViewerToolWindow>();
            window?.FilterVmInstanceLog(Instance.Id.ToString());
        }

        private async Task OnAttachDebuggerAsync()
        {
            AttachDebuggerWindow.PromptUser(_instance);
            // Refresh instance state because the firewall rules may have been changed.
            await RefreshInstanceStateAsync();
        }

        private void OnSavePublishSettingsCommand()
        {
            Debug.WriteLine($"Generating Publishing settings for {Instance.Name}");

            var credentials = WindowsCredentialsChooserWindow.PromptUser(
                Instance,
                new WindowsCredentialsChooserWindow.Options
                {
                    Title = Resources.CloudExplorerGceSavePubSettingsCredentialsTitle,
                    Message = Resources.CloudExplorerGceSavePubSettingsCredentialsMessage,
                    ActionButtonCaption = Resources.UiSaveButtonCaption
                });
            if (credentials == null)
            {
                Debug.WriteLine("User canceled when selecting credentials.");
                return;
            }

            var projectId = CredentialsStore.Default.CurrentProjectId;
            var storePath = PromptForPublishSettingsPath($"{projectId}-{Instance.Name}-{credentials.User}");
            if (storePath == null)
            {
                Debug.WriteLine("User canceled saving the pubish settings.");
                return;
            }

            EventsReporterWrapper.ReportEvent(SavePublishSettingsEvent.Create());
            var profile = Instance.GeneratePublishSettings(
                userName: credentials.User,
                password: credentials.Password);
            File.WriteAllText(storePath, profile);
            GcpOutputWindow.Default.OutputLine(String.Format(Resources.CloudExplorerGcePublishingSettingsSavedMessage, storePath));
        }

        private void OnManageWindowsCredentialsCommand()
        {
            ManageWindowsCredentialsWindow.PromptUser(Instance);
        }

        private void OnOpenOnCloudConsoleCommand()
        {
            EventsReporterWrapper.ReportEvent(OpenGceInstanceOnCloudConsoleEvent.Create());

            var url = $"https://console.cloud.google.com/compute/instancesDetail/zones/{_instance.GetZoneName()}/instances/{_instance.Name}?project={_owner.Context.CurrentProject.ProjectId}";
            Process.Start(url);
        }

        private async Task OnPropertiesWindowCommandAsync()
        {
            await _owner.Context.ShowPropertiesWindowAsync(Item);
        }

        private async Task OnManageFirewallPortsCommandAsync()
        {
            try
            {
                var changes = PortManagerWindow.PromptUser(Instance);
                if (changes?.HasChanges ?? false)
                {
                    var operation = _owner.DataSource.UpdateInstancePorts(
                        Instance,
                        portsToEnable: changes.PortsToEnable,
                        portsToDisable: changes.PortsToDisable);
                    await UpdateInstanceStateAsync(operation);

                    EventsReporterWrapper.ReportEvent(ChangedFirewallPortsEvent.Create(CommandStatus.Success));
                }
            }
            catch (DataSourceException)
            {
                EventsReporterWrapper.ReportEvent(ChangedFirewallPortsEvent.Create(CommandStatus.Failure));
                UserPromptService.Default.ErrorPrompt(Resources.CloudExplorerGceFailedToUpdateFirewallMessage, Resources.CloudExplorerGceFailedToUpdateFirewallCaption);
            }
        }

        private async Task OnStopInstanceCommandAsync()
        {
            try
            {
                if (!UserPromptService.Default.ActionPrompt(
                    String.Format(Resources.CloudExplorerGceStopInstanceConfirmationPrompt, Instance.Name),
                    String.Format(Resources.CloudExplorerGceStopInstanceConfirmationPromptCaption, Instance.Name),
                    actionCaption: Resources.UiStopButtonCaption))
                {
                    Debug.WriteLine($"The user cancelled stopping instance {Instance.Name}.");
                    return;
                }

                var operation = _owner.DataSource.StopInstance(Instance);
                await UpdateInstanceStateAsync(operation);

                EventsReporterWrapper.ReportEvent(StopGceInstanceEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException ex)
            {
                await GcpOutputWindow.Default.ActivateAsync();
                await GcpOutputWindow.Default.OutputLineAsync(String.Format(Resources.CloudExplorerGceFailedToStopInstanceMessage, Instance.Name, ex.Message));
                EventsReporterWrapper.ReportEvent(StopGceInstanceEvent.Create(CommandStatus.Failure));
            }
        }

        private static void ShowOAuthErrorDialog(OAuthException ex)
        {
            Debug.WriteLine($"Failed to fetch oauth credentials: {ex.Message}");
            UserPromptService.Default.OkPrompt(
                String.Format(Resources.CloudExplorerGceFailedToGetOauthCredentialsMessage, CredentialsStore.Default.CurrentAccount.AccountName),
                Resources.CloudExplorerGceFailedToGetOauthCredentialsCaption);
        }

        private async Task OnStartInstanceCommandAsync()
        {
            try
            {
                if (!UserPromptService.Default.ActionPrompt(
                    String.Format(Resources.CloudExplorerGceStartInstanceConfirmationPrompt, Instance.Name),
                    String.Format(Resources.CloudExplorerGceStartInstanceConfirmationPromptCaption, Instance.Name),
                    actionCaption: Resources.UiStartButtonCaption))
                {
                    Debug.WriteLine($"The user cancelled starting instance {Instance.Name}.");
                    return;
                }

                var operation = _owner.DataSource.StartInstance(Instance);
                await UpdateInstanceStateAsync(operation);

                EventsReporterWrapper.ReportEvent(StartGceInstanceEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException ex)
            {
                await GcpOutputWindow.Default.ActivateAsync();
                await GcpOutputWindow.Default.OutputLineAsync(String.Format(Resources.CloudExplorerGceFailedToStartInstanceMessage, Instance.Name, ex.Message));

                EventsReporterWrapper.ReportEvent(StartGceInstanceEvent.Create(CommandStatus.Failure));
            }
        }

        private void OnOpenTerminalServerSessionCommand()
        {
            var credentials = WindowsCredentialsChooserWindow.PromptUser(
                _instance,
                new WindowsCredentialsChooserWindow.Options
                {
                    Title = Resources.TerminalServerManagerWindowTitle,
                    Message = Resources.TerminalServerManagerWindowMessage,
                    ActionButtonCaption = Resources.UiOpenButtonCaption
                });
            if (credentials != null)
            {
                EventsReporterWrapper.ReportEvent(StartRemoteDesktopSessionEvent.Create());
                TerminalServerManager.OpenSession(_instance, credentials);
            }
        }

        private void OnOpenWebsite()
        {
            EventsReporterWrapper.ReportEvent(OpenGceInstanceWebsiteEvent.Create());

            var url = Instance.GetDestinationAppUri();
            Debug.WriteLine($"Opening Web Site: {url}");
            Process.Start(url);
        }

        private static string PromptForPublishSettingsPath(string fileName)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = Resources.CloudExplorerGceSavePublishSettingsDialogCaption;
            dialog.FileName = fileName;
            dialog.DefaultExt = ".publishsettings";
            dialog.Filter = Resources.CloudExplorerGceSavePublishSettingsExtensions;
            dialog.InitialDirectory = GetDownloadsPath();
            dialog.OverwritePrompt = true;

            var result = dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }

        private static string GetDownloadsPath()
        {
            return Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders",
                "{374DE290-123F-4565-9164-39C4925E467B}",
                String.Empty).ToString();
        }

        private void UpdateIcon()
        {
            switch (Instance.Status)
            {
                case InstanceExtensions.RunningStatus:
                    Icon = s_instanceRunningIcon.Value;
                    break;

                case InstanceExtensions.TerminatedStatus:
                    Icon = s_instanceStopedIcon.Value;
                    break;

                default:
                    Icon = s_instanceTransitionIcon.Value;
                    break;
            }
        }
    }
}
