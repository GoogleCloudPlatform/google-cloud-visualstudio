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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.TerminalServer;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.WindowsCredentialsChooser;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections;
using System.Linq;
using System.Windows;
using GoogleCloudExtension.GCloud;

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

            UpdateInstanceState();
        }

        private void UpdateInstanceState()
        {
            GceOperation pendingOperation = _owner.DataSource.GetPendingOperation(Instance);
            UpdateInstanceState(pendingOperation);
        }

        private async void UpdateInstanceState(GceOperation pendingOperation)
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
                    // Await the end of the task. We can also get here if the task is faulted, 
                    // in which case we need to handle that case.
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

                    // Refresh the instance state after the operation is finished.
                    Instance = await _owner.DataSource.RefreshInstance(Instance);
                }
                catch (DataSourceException ex)
                {
                    Caption = Instance.Name;
                    IsLoading = false;
                    IsError = true;
                    UpdateContextMenu();

                    Debug.WriteLine($"Previous operation failed.");
                    switch (pendingOperation.OperationType)
                    {
                        case OperationType.StartInstance:
                            GcpOutputWindow.OutputLine(String.Format(Resources.CloudExplorerGceStartOperationFailedMessage, Instance.Name, ex.Message));
                            break;

                        case OperationType.StopInstance:
                            GcpOutputWindow.OutputLine(String.Format(Resources.CloudExplorerGceStopOperationFailedMessage, Instance.Name, ex.Message));
                            break;
                    }

                    // Permanent error.
                    return;
                }
                catch (OAuthException ex)
                {
                    ShowOAuthErrorDialog(ex);
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

        private void UpdateContextMenu()
        {
            // If the instance is busy, then there's no context menu.
            // TODO(ivann): Should we have a "Cancel Operation" menu item?
            if (IsLoading || IsError)
            {
                ContextMenu = null;
                return;
            }

            var openWebSite = new WeakCommand(OnOpenWebsite, Instance.IsAspnetInstance() && Instance.IsRunning());
            var openTerminalServerSessionCommand = new WeakCommand(
                OnOpenTerminalServerSessionCommand,
                Instance.IsWindowsInstance() && Instance.IsRunning());
            var startInstanceCommand = new WeakCommand(OnStartInstanceCommand);
            var stopInstanceCommand = new WeakCommand(OnStopInstanceCommand);
            var manageFirewallPorts = new WeakCommand(OnManageFirewallPortsCommand);
            var manageWindowsCredentials = new WeakCommand(OnManageWindowsCredentialsCommand, canExecuteCommand: Instance.IsWindowsInstance());

            var publishMenuItem = new MenuItem { Header = Resources.CloudExplorerGceSavePublishSettingsMenuHeader };
            publishMenuItem.ItemsSource = new List<MenuItem>
            {
                new MenuItem { Header = "Choose credentials...", Command=new WeakCommand(OnDownloadPublishSettingsWithCredentialsCommand)},
                new MenuItem { Header = "No Credentials", Command=new WeakCommand(OnDownloadPublishSettingsWithoutCredentialsCommand)},
            };

            var menuItems = new List<MenuItem>
            {
                publishMenuItem,
                new MenuItem { Header = Resources.CloudExplorerGceOpenTerminalSessionMenuHeader, Command = openTerminalServerSessionCommand },
                new MenuItem { Header = Resources.CloudExplorerGceOpenWebSiteMenuHeader, Command = openWebSite },
                new MenuItem { Header = Resources.CloudExplorerGceManageFirewallPortsMenuHeader, Command = manageFirewallPorts },
                new MenuItem { Header = Resources.CloudExplorerGceManageWindowsCredentialsMenuHeader, Command = manageWindowsCredentials }
            };

            if (Instance.IsRunning())
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceStopInstanceMenuHeader, Command = stopInstanceCommand });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = Resources.CloudExplorerGceStartInstanceMenuHeader, Command = startInstanceCommand });
            }

            menuItems.Add(new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new WeakCommand(OnOpenOnCloudConsoleCommand) });
            menuItems.Add(new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new WeakCommand(OnPropertiesWindowCommand) });

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnDownloadPublishSettingsWithoutCredentialsCommand()
        {
            ExtensionAnalytics.ReportCommand(CommandName.GetPublishSettingsForGceInstance, CommandInvocationSource.Button);

            Debug.WriteLine($"Generating Publishing settings for {Instance.Name}");

            var storePath = PromptForPublishSettingsPath(Instance.Name);
            if (storePath == null)
            {
                Debug.WriteLine("User canceled saving the pubish settings.");
                return;
            }

            var profile = Instance.GeneratePublishSettings();
            File.WriteAllText(storePath, profile);
            GcpOutputWindow.OutputLine(String.Format(Resources.CloudExplorerGcePublishingSettingsSavedMessage, storePath));
        }

        private void OnDownloadPublishSettingsWithCredentialsCommand()
        {
            Debug.WriteLine($"Generating Publishing settings for {Instance.Name}");

            var credentials = WindowsCredentialsChooserWindow.PromptUser(
                Instance,
                new WindowsCredentialsChooserWindow.Options
                {
                    Title = "Choose credentials",
                    Message = "Credentials for publish settings"
                });
            if (credentials == null)
            {
                Debug.WriteLine("User canceled when selecting credentials.");
                return;
            }
                
            var storePath = PromptForPublishSettingsPath(Instance.Name);
            if (storePath == null)
            {
                Debug.WriteLine("User canceled saving the pubish settings.");
                return;
            }

            var profile = Instance.GeneratePublishSettings(
                userName: credentials.User,
                password: credentials.Password);
            File.WriteAllText(storePath, profile);
            GcpOutputWindow.OutputLine(String.Format(Resources.CloudExplorerGcePublishingSettingsSavedMessage, storePath));
        }

        private void OnManageWindowsCredentialsCommand()
        {
            ManageWindowsCredentialsWindow.PromptUser(Instance);
        }

        private void OnOpenOnCloudConsoleCommand()
        {
            var url = $"https://console.cloud.google.com/compute/instancesDetail/zones/{_instance.GetZoneName()}/instances/{_instance.Name}?project={_owner.Context.CurrentProject.ProjectId}";
            Process.Start(url);
        }

        private void OnPropertiesWindowCommand()
        {
            _owner.Context.ShowPropertiesWindow(Item);
        }

        private void OnManageFirewallPortsCommand()
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
                    UpdateInstanceState(operation);
                }
            }
            catch (DataSourceException)
            {
                UserPromptUtils.ErrorPrompt(Resources.CloudExplorerGceFailedToUpdateFirewallMessage, Resources.CloudExplorerGceFailedToUpdateFirewallCaption);
            }
        }

        private void OnStopInstanceCommand()
        {
            try
            {
                if (!UserPromptUtils.YesNoPrompt(
                    String.Format(Resources.CloudExplorerGceStopInstanceConfirmationPrompt, Instance.Name),
                    String.Format(Resources.CloudExplorerGceStopInstanceConfirmationPromptCaption, Instance.Name)))
                {
                    Debug.WriteLine($"The user cancelled stopping instance {Instance.Name}.");
                    return;
                }

                var operation = _owner.DataSource.StopInstance(Instance);
                UpdateInstanceState(operation);
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine(String.Format(Resources.CloudExplorerGceFailedToStopInstanceMessage, Instance.Name, ex.Message));
            }
            catch (OAuthException ex)
            {
                ShowOAuthErrorDialog(ex);
            }
        }

        private static void ShowOAuthErrorDialog(OAuthException ex)
        {
            Debug.WriteLine($"Failed to fetch oauth credentials: {ex.Message}");
            UserPromptUtils.OkPrompt(
                String.Format(Resources.CloudExplorerGceFailedToGetOauthCredentialsMessage, CredentialsStore.Default.CurrentAccount.AccountName),
                Resources.CloudExplorerGceFailedToGetOauthCredentialsCaption);
        }

        private void OnStartInstanceCommand()
        {
            try
            {
                if (!UserPromptUtils.YesNoPrompt(
                    String.Format(Resources.CloudExplorerGceStartInstanceConfirmationPrompt, Instance.Name),
                    String.Format(Resources.CloudExplorerGceStartInstanceConfirmationPromptCaption, Instance.Name)))
                {
                    Debug.WriteLine($"The user cancelled starting instance {Instance.Name}.");
                    return;
                }

                var operation = _owner.DataSource.StartInstance(Instance);
                UpdateInstanceState(operation);
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine(String.Format(Resources.CloudExplorerGceFailedToStartInstanceMessage, Instance.Name, ex.Message));
            }
            catch (OAuthException ex)
            {
                ShowOAuthErrorDialog(ex);
            }
        }

        private void OnOpenTerminalServerSessionCommand()
        {
            ExtensionAnalytics.ReportCommand(CommandName.OpenTerminalServerSessionForGceInstanceCommand, CommandInvocationSource.Button);

            var credentials = WindowsCredentialsChooserWindow.PromptUser(
                _instance,
                new WindowsCredentialsChooserWindow.Options
                {
                    Title = Resources.TerminalServerManagerWindowTitle,
                    Message = Resources.TerminalServerManagerWindowMessage
                });
            if (credentials != null)
            {
                TerminalServerManager.OpenSession(_instance, credentials);
            }
        }

        private void OnOpenWebsite()
        {
            ExtensionAnalytics.ReportCommand(CommandName.OpenWebsiteForGceInstanceCommand, CommandInvocationSource.Button);

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