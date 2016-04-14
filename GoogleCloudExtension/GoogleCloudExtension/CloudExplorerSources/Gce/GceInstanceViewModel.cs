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
        private readonly WeakCommand _openTerminalServerSessionCommand;

        public GceInstanceViewModel(GceSourceRootViewModel owner, GceInstance instance)
        {
            Content = instance.Name;
            Icon = s_instanceIcon.Value;

            _owner = owner;
            _instance = instance;

            _getPublishSettingsCommand = new WeakCommand(OnGetPublishSettings, _instance.IsAspnetInstance() && _instance.IsRunning());
            _openWebSite = new WeakCommand(OnOpenWebsite, _instance.IsAspnetInstance() && _instance.IsRunning());
            _openTerminalServerSessionCommand = new WeakCommand(OnOpenTerminalServerSessionCommand, _instance.IsWindowsInstance());

            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Save Publishing Settings...", Command = _getPublishSettingsCommand },
                new MenuItem {Header="Open Terminal Server Session...", Command = _openTerminalServerSessionCommand },
                new MenuItem {Header="Open Web Site...", Command = _openWebSite },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
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