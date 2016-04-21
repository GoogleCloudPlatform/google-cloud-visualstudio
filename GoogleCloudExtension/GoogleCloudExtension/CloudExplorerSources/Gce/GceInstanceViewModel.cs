// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private static readonly TimeSpan s_pollTimeout = new TimeSpan(0, 0, 10);

        private const string IconRunningResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_running.png";
        private const string IconStopedResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_stoped.png";
        private const string IconTransitionResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_transition.png";

        private static readonly Lazy<ImageSource> s_instanceRunningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconRunningResourcePath));
        private static readonly Lazy<ImageSource> s_instanceStopedIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconStopedResourcePath));
        private static readonly Lazy<ImageSource> s_instanceTransitionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconTransitionResourcePath));

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
                if (Instance.IsGaeInstance())
                {
                    return new GceGaeInstanceItem(Instance);
                }
                else if (Instance.IsSqlServer())
                {
                    return new AspNetInstanceItem(Instance);
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
                        Content = $"Starting instance {Instance.Name}";
                        break;

                    case OperationType.StopInstance:
                        Content = $"Stoping instance {Instance.Name}";
                        break;

                    case OperationType.StoreMetadata:
                        Content = $"Storing metadata {Instance.Name}";
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
                catch (ZoneOperationException ex)
                {
                    Content = Instance.Name;
                    IsLoading = false;
                    IsError = true;
                    UpdateContextMenu();

                    Debug.WriteLine($"Previous operation failed.");
                    switch (pendingOperation.OperationType)
                    {
                        case OperationType.StartInstance:
                            GcpOutputWindow.OutputLine($"Start instance operation for {Instance.Name} failed. {ex.Message}");
                            break;

                        case OperationType.StopInstance:
                            GcpOutputWindow.OutputLine($"Stop instance operation for {Instance.Name} failed. {ex.Message}");
                            break;

                        case OperationType.StoreMetadata:
                            GcpOutputWindow.OutputLine($"Store metadata operation for {Instance.Name} failed. {ex.Message}");
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
            Content = Instance.Name;
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

            var getPublishSettingsCommand = new WeakCommand(OnGetPublishSettings, Instance.IsAspnetInstance() && Instance.IsRunning());
            var openWebSite = new WeakCommand(OnOpenWebsite, Instance.IsAspnetInstance() && Instance.IsRunning());
            var openTerminalServerSessionCommand = new WeakCommand(
                OnOpenTerminalServerSessionCommand,
                Instance.IsWindowsInstance() && Instance.IsRunning());
            var startInstanceCommand = new WeakCommand(OnStartInstanceCommand);
            var stopInstanceCommand = new WeakCommand(OnStopInstanceCommand);

            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Save Publishing Settings...", Command = getPublishSettingsCommand },
                new MenuItem {Header="Open Terminal Server Session...", Command = openTerminalServerSessionCommand },
                new MenuItem {Header="Open Web Site...", Command = openWebSite },
            };

            if (Instance.IsRunning())
            {
                menuItems.Add(new MenuItem { Header = "Stop instance...", Command = stopInstanceCommand });
            }
            else
            {
                menuItems.Add(new MenuItem { Header = "Start instance...", Command = startInstanceCommand });
            }

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private async void OnStopInstanceCommand()
        {
            try
            {
                if (!UserPromptUtils.YesNoPrompt(
                    $"Are you sure you want to stop instance {Instance.Name}?",
                    $"Stop {Instance.Name}"))
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
                GcpOutputWindow.OutputLine($"Failed to stop instance {Instance.Name}. {ex.Message}");
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
                $"Failed to fetch oauth credentials for account {AccountsManager.CurrentAccount.AccountName}, please login again.",
                "Credentials Error");
        }

        private async void OnStartInstanceCommand()
        {
            try
            {
                if (!UserPromptUtils.YesNoPrompt(
                    $"Are you sure you want to start instance {Instance.Name}?",
                    $"Start {Instance.Name}"))
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
                GcpOutputWindow.OutputLine($"Failed to start instance {Instance.Name}. {ex.Message}");
            }
            catch (OAuthException ex)
            {
                ShowOAuthErrorDialog(ex);
            }
        }

        private void OnOpenTerminalServerSessionCommand()
        {
            Process.Start("mstsc", $"/v:{Instance.GetPublicIpAddress()}");
        }

        private void OnOpenWebsite()
        {
            var url = Instance.GetDestinationAppUri();
            Debug.WriteLine($"Opening Web Site: {url}");
            Process.Start(url);
        }

        private void OnGetPublishSettings()
        {
            Debug.WriteLine($"Generating Publishing settings for {Instance.Name}");

            var storePath = PromptForPublishSettingsPath(Instance.Name);
            if (storePath == null)
            {
                Debug.WriteLine("User canceled saving the pubish settings.");
                return;
            }

            var profile = Instance.GeneratePublishSettings();
            File.WriteAllText(storePath, profile);
            GcpOutputWindow.OutputLine($"Publishsettings saved to {storePath}");
        }

        private static string PromptForPublishSettingsPath(string fileName)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Save Publish Settings";
            dialog.FileName = fileName;
            dialog.DefaultExt = ".publishsettings";
            dialog.Filter = "Publish Settings file (*.publishsettings)|*.publishsettings";
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