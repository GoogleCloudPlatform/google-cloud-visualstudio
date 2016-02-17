using System;
using GoogleCloudExtension.CloudExplorer;
using System.Windows.Media;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.CloudExplorerSources.Gce;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Controls;
using GoogleCloudExtension.Projects;
using System.IO;
using Microsoft.Win32;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
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

        private void OnGetPublishSettings()
        {
            Debug.WriteLine($"Generating Publishing settings for {_instance.Name}");
            var credentials = _instance.GetServerCredentials();
            if (credentials == null)
            {
                return;
            }

            var doc = new XDocument(
                new XElement("publishData",
                    new XElement("publishProfile",
                        new XAttribute("profileName", "Google Cloud Profile-WebDeploy"),
                        new XAttribute("publishMethod", "MSDeploy"),
                        new XAttribute("publishUrl", _instance.GetPublishUrl()),
                        new XAttribute("msdeploySite", "Default Web Site"),
                        new XAttribute("userName", credentials.User),
                        new XAttribute("userPWD", credentials.Password),
                        new XAttribute("destinationAppUri", _instance.GetDestinationAppUri()))));

            var profile = doc.ToString();
            GcpOutputWindow.OutputLine($"Generated profile: {profile}");

            var downloadsPath = GetDownloadsPath();
            var settingsPath = Path.Combine(downloadsPath, $"{_instance.Name}.publishsettings");
            File.WriteAllText(settingsPath, doc.ToString());
            GcpOutputWindow.OutputLine($"Publishsettings saved to {settingsPath}");
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