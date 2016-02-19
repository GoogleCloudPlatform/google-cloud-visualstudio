// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private const string GcpIisUser = "gcpiisuser";
        private static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly GceInstance _instance;
        private readonly WeakCommand _getPublishSettingsCommand;
        private readonly WeakCommand _openWebSite;

        public GceInstanceViewModel(GceInstance instance)
        {
            Content = instance.Name;
            Icon = s_instanceIcon.Value;
            _instance = instance;

            _getPublishSettingsCommand = new WeakCommand(OnGetPublishSettings, _instance.IsAspnetInstance());
            _openWebSite = new WeakCommand(OnOpenWebsite, _instance.IsAspnetInstance());

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
            Debug.WriteLine($"Generating Publishing settings for {_instance.Name}");
            var credentials = await EnsureWindowsCredentials(_instance);

            var profile = _instance.GeneratePublishSettings();
            if (profile == null)
            {
                GcpOutputWindow.OutputLine($"No .publishsettings could be generated for {_instance.Name}");
                return;
            }

            GcpOutputWindow.OutputLine($"Generated .publishsettings: {profile}");
            var downloadsPath = GetDownloadsPath();
            var settingsPath = Path.Combine(downloadsPath, $"{_instance.Name}.publishsettings");
            File.WriteAllText(settingsPath, profile);
            GcpOutputWindow.OutputLine($"Publishsettings saved to {settingsPath}");
        }

        private static async Task<GceCredentials> EnsureWindowsCredentials(GceInstance instance)
        {
            var existingCredentials = instance.GetServerCredentials();
            if (existingCredentials != null)
            {
                return existingCredentials;
            }

            GcpOutputWindow.OutputLine($"Creating new credentials for {instance.Name}...");
            var newCredentials = await GCloudWrapper.Instance.ResetWindowsCredentials(instance.Name, instance.ZoneName, GcpIisUser);
            var result = new GceCredentials
            {
                User = newCredentials.User,
                Password = newCredentials.Password
            };

            GcpOutputWindow.OutputLine($"Storing new credentials for {instance.Name}...");
            var oauthToken = await GCloudWrapper.Instance.GetAccessTokenAsync();
            await instance.SetServerCredentials(result, oauthToken);
            GcpOutputWindow.OutputLine($"Credentials for {instance.Name} stored.");

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