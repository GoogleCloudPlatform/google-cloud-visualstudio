// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using GoogleCloudExtension.OAuth;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private static readonly TimeSpan s_pollTimeout = new TimeSpan(0, 0, 10);

        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon.png";
        private const string GcpIisUser = "gcpiisuser";
        private static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly GceSourceRootViewModel _owner;
        private GceInstance _instance;  // This is not readonly because it can change if starting/stopping.

        public object Item
        {
            get
            {
                if (_instance.IsGaeInstance())
                {
                    return new GceGaeInstanceItem(_instance);
                }
                else if (_instance.IsSqlServer())
                {
                    return new AspNetInstanceItem(_instance);
                }
                else
                {
                    return new GceInstanceItem(_instance);
                }
            }
            private set
            {
                _instance = (GceInstance)value;
                ItemChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ItemChanged;

        public GceInstanceViewModel(GceSourceRootViewModel owner, GceInstance instance)
        {
            Icon = s_instanceIcon.Value;

            _owner = owner;
            _instance = instance;

            UpdateInstanceState();
        }

        private void UpdateInstanceState()
        {
            GceOperation pendingOperation = GceDataSource.GetPendingOperation(_instance);
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
                        Content = $"Starting instance {_instance.Name}";
                        break;

                    case OperationType.StopInstance:
                        Content = $"Stoping instance {_instance.Name}";
                        break;

                    case OperationType.StoreMetadata:
                        Content = $"Storing metadata {_instance.Name}";
                        break;
                }

                // Update the context menu to reflect the state.
                UpdateContextMenu();

                try
                {
                    var oauthToken = await AccountsManager.GetAccessTokenAsync();

                    // Await the end of the task. We can also get here if the task is faulted, 
                    // in which case we need to handle that case.
                    while (true)
                    {
                        // Refresh the instance before waiting for the operation to finish.
                        Item = await GceDataSource.RefreshInstance(_instance, oauthToken);

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
                    Item = await GceDataSource.RefreshInstance(_instance, oauthToken);
                }
                catch (ZoneOperationException ex)
                {
                    Content = _instance.Name;
                    IsLoading = false;
                    IsError = true;
                    UpdateContextMenu();

                    Debug.WriteLine($"Previous operation failed.");
                    switch (pendingOperation.OperationType)
                    {
                        case OperationType.StartInstance:
                            GcpOutputWindow.OutputLine($"Start instance operation for {_instance.Name} failed. {ex.Message}");
                            break;

                        case OperationType.StopInstance:
                            GcpOutputWindow.OutputLine($"Stop instance operation for {_instance.Name} failed. {ex.Message}");
                            break;

                        case OperationType.StoreMetadata:
                            GcpOutputWindow.OutputLine($"Store metadata operation for {_instance.Name} failed. {ex.Message}");
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
                pendingOperation = GceDataSource.GetPendingOperation(_instance);
            }

            // Normal state, no pending operations.
            IsLoading = false;
            Content = _instance.Name;
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

            var getPublishSettingsCommand = new WeakCommand(OnGetPublishSettings, _instance.IsAspnetInstance() && _instance.IsRunning());
            var openWebSite = new WeakCommand(OnOpenWebsite, _instance.IsAspnetInstance() && _instance.IsRunning());
            var openTerminalServerSessionCommand = new WeakCommand(
                OnOpenTerminalServerSessionCommand,
                _instance.IsWindowsInstance() && _instance.IsRunning());
            var startInstanceCommand = new WeakCommand(OnStartInstanceCommand);
            var stopInstanceCommand = new WeakCommand(OnStopInstanceCommand);

            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Save Publishing Settings...", Command = getPublishSettingsCommand },
                new MenuItem {Header="Open Terminal Server Session...", Command = openTerminalServerSessionCommand },
                new MenuItem {Header="Open Web Site...", Command = openWebSite },
            };

            if (_instance.IsRunning())
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
                    $"Are you sure you want to stop instance {_instance.Name}?",
                    $"Stop {_instance.Name}"))
                {
                    Debug.WriteLine($"The user cancelled stopping instance {_instance.Name}.");
                    return;
                }

                var oauthToken = await AccountsManager.GetAccessTokenAsync();
                var operation = GceDataSource.StopInstance(_instance, oauthToken);
                UpdateInstanceState(operation);
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine($"Failed to stop instance {_instance.Name}. {ex.Message}");
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
                    $"Are you sure you want to start instance {_instance.Name}?",
                    $"Start {_instance.Name}"))
                {
                    Debug.WriteLine($"The user cancelled starting instance {_instance.Name}.");
                    return;
                }

                var oauthToken = await AccountsManager.GetAccessTokenAsync();
                var operation = GceDataSource.StartInstance(_instance, oauthToken);
                UpdateInstanceState(operation);
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine($"Failed to start instance {_instance.Name}. {ex.Message}");
            }
            catch (OAuthException ex)
            {
                ShowOAuthErrorDialog(ex);
            }
        }

        private void OnOpenTerminalServerSessionCommand()
        {
            Process.Start("mstsc", $"/v:{_instance.GetPublicIpAddress()}");
        }

        private void OnOpenWebsite()
        {
            var url = _instance.GetDestinationAppUri();
            Debug.WriteLine($"Opening Web Site: {url}");
            Process.Start(url);
        }

        private void OnGetPublishSettings()
        {
            Debug.WriteLine($"Generating Publishing settings for {_instance.Name}");

            var storePath = PromptForPublishSettingsPath(_instance.Name);
            if (storePath == null)
            {
                Debug.WriteLine("User canceled saving the pubish settings.");
                return;
            }

            var profile = _instance.GeneratePublishSettings();
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
    }
}