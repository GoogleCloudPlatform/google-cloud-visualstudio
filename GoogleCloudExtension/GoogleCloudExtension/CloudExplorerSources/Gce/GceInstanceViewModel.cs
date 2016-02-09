using System;
using GoogleCloudExtension.CloudExplorer;
using System.Windows.Media;
using GoogleCloudExtension.Utils;
using System.Linq;
using System.Diagnostics;

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

            _getPublishSettingsCommand = new WeakCommand(OnGetPublishSettings, IsAspnetInstance(_instance));
        }

        private void OnGetPublishSettings()
        {
            Debug.WriteLine($"Generating Publishing settings for {_instance.Name}");

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

        private static bool IsAspnetInstance(GceInstance instance)
        {
            return true;
        }
    }      
}