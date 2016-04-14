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
        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon.png";
        private const string GcpIisUser = "gcpiisuser";
        private static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly GceSourceRootViewModel _owner;
        private GceInstance _instance;  // This is not readonly because it can change if starting/stopping.

        public GceInstanceViewModel(GceSourceRootViewModel owner, GceInstance instance)
        {
            Content = instance.Name;
            Icon = s_instanceIcon.Value;

            _owner = owner;
            _instance = instance;

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

                // Transition into busy state.
                Content = $"Stopping {_instance.Name}...";
                IsLoading = true;
                UpdateContextMenu();
                
                var oauthToken = await AccountsManager.GetAccessTokenAsync();

                // Stop the instance and wait for finish.
                await GceDataSource.StopInstance(_instance, oauthToken);

                // Refresh the instance.
                _instance = await GceDataSource.RefreshInstance(_instance, oauthToken);
                Content = _instance.Name;
                IsLoading = false;
                UpdateContextMenu();
            }
            catch (DataSourceException ex)
            {
                // Transition back to the normal state.
                Content = _instance.Name;
                IsLoading = false;
                UpdateContextMenu();

                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine($"Failed to stop instance {_instance.Name}. {ex.Message}");
            }
            catch (OAuthException ex)
            {
                // Transition into full error state.
                Content = $"Authentication failed, {_instance.Name}";
                IsError = true;
                UpdateContextMenu();

                Debug.WriteLine($"Failed to fetch oauth credentials: {ex.Message}");
                UserPromptUtils.OkPrompt(
                    $"Failed to fetch oauth credentials for account {AccountsManager.CurrentAccount.AccountName}, please login again.",
                    "Credentials Error");
            }
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

                // Transition the state to busy.
                Content = $"Starting {_instance.Name}...";
                IsLoading = true;
                UpdateContextMenu();

                var oauthToken = await AccountsManager.GetAccessTokenAsync();

                // Start the instance, wait for the operation to complete.
                await GceDataSource.StartInstance(_instance, oauthToken);

                // Refresh the instance, to get the new state, and update the context menu for the
                // new state.
                _instance = await GceDataSource.RefreshInstance(_instance, oauthToken);
                Content = _instance.Name;
                IsLoading = false;
                UpdateContextMenu();
            }
            catch (DataSourceException ex)
            {
                // Transition back to normal, the user can try again.
                Content = _instance.Name;
                IsLoading = false;
                UpdateContextMenu();

                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine($"Failed to start instance {_instance.Name}. {ex.Message}");
            }
            catch (OAuthException ex)
            {
                // Transition into full error state.
                Content = $"Failed authentication, {_instance.Name}";
                IsError = true;
                UpdateContextMenu();

                Debug.WriteLine($"Failed to fetch oauth credentials: {ex.Message}");
                UserPromptUtils.OkPrompt(
                    $"Failed to fetch oauth credentials for account {AccountsManager.CurrentAccount.AccountName}, please login again.",
                    "Credentials Error");
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