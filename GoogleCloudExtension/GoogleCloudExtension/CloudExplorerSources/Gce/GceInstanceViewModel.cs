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

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon.png";
        private const string GcpIisUser = "gcpiisuser";
        private static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly GceSourceRootViewModel _owner;
        private GceInstance _instance;  // This is not readonly because it can change if resetting the password.
        private readonly WeakCommand _getPublishSettingsCommand;
        private readonly WeakCommand _openWebSite;

        public GceInstanceViewModel(GceSourceRootViewModel owner, GceInstance instance)
        {
            Content = instance.Name;
            Icon = s_instanceIcon.Value;

            _owner = owner;
            _instance = instance;

            _getPublishSettingsCommand = new WeakCommand(OnGetPublishSettings, _instance.IsAspnetInstance() && _instance.IsRunning());
            _openWebSite = new WeakCommand(OnOpenWebsite, _instance.IsAspnetInstance() && _instance.IsRunning());

            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Get Publishing Settings", Command = _getPublishSettingsCommand },
                new MenuItem {Header="Open Web Site", Command = _openWebSite },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnOpenWebsite()
        {
            var url = _instance.GetDestinationAppUri();
            Debug.WriteLine($"Opening Web Site: {url}");
            Process.Start(url);
        }

        private async void OnGetPublishSettings()
        {
            //try
            //{
            //    Debug.WriteLine($"Generating Publishing settings for {_instance.Name}");
            //    var credentials = await EnsureWindowsCredentials();

            //    var profile = _instance.GeneratePublishSettings(credentials);
            //    if (profile == null)
            //    {
            //        GcpOutputWindow.OutputLine($"No .publishsettings could be generated for {_instance.Name}");
            //        return;
            //    }

            //    GcpOutputWindow.OutputLine($"Generated .publishsettings: {profile}");
            //    var downloadsPath = GetDownloadsPath();
            //    var settingsPath = Path.Combine(downloadsPath, $"{_instance.Name}.publishsettings");
            //    File.WriteAllText(settingsPath, profile);
            //    GcpOutputWindow.OutputLine($"Publishsettings saved to {settingsPath}");
            //}
            //catch (GCloudException ex)
            //{
            //    GcpOutputWindow.OutputLine($"Failed to reset credentials for {_instance.Name}: {ex.Message}");
            //}
        }

        private async Task<GceCredentials> EnsureWindowsCredentials()
        {
            var existingCredentials = _instance.GetServerCredentials();
            if (existingCredentials != null)
            {
                return existingCredentials;
            }

            GcpOutputWindow.OutputLine($"Creating new credentials for {_instance.Name}...");
            var oauthToken = await AccountsManager.GetAccessTokenAsync();
            var newCredentials = await GceDataSource.ResetWindowsCredentials(
                _owner.Owner.CurrentProject.Id,
                zoneName: _instance.ZoneName,
                name: _instance.Name,
                userName: GcpIisUser,
                oauthToken: oauthToken);
            var result = new GceCredentials
            {
                User = newCredentials.User,
                Password = newCredentials.Password
            };

            try
            {
                GcpOutputWindow.OutputLine($"Storing new credentials for {_instance.Name}...");
                _instance = await _instance.SetServerCredentials(result, oauthToken);
                GcpOutputWindow.OutputLine($"Credentials for {_instance.Name} stored.");
            }
            catch (ZoneOperationException ex)
            {
                GcpOutputWindow.OutputLine($"Failed to store credentials: {ex.Error.Errors.FirstOrDefault()?.Message}");
            }

            return result;
        }

        public object Item
        {
            get
            {
                if (_instance.IsGaeInstance())
                {
                    return new GceGaeInstanceItem(_instance);
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