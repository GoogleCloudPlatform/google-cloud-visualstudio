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

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly GceInstance _instance;
        private readonly WeakCommand _getPublishSettingsCommand;

        public GceInstanceViewModel(GceInstance instance)
        {
            Content = instance.Name;
            Icon = s_instanceIcon.Value;
            _instance = instance;

            _getPublishSettingsCommand = new WeakCommand(OnGetPublishSettings, _instance.IsAspnetInstance());
            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Get Publishing Settings", Command = _getPublishSettingsCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
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

            var solution = SolutionHelper.CurrentSolution;
            var solutionDir = Path.GetDirectoryName(solution.Root);
            var path = Path.Combine(solutionDir, $"{_instance.Name}.publishsettings");
            File.WriteAllText(path, doc.ToString());
            GcpOutputWindow.OutputLine($"Publishsettings saved to {path}");
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
    }
}