using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/app_engine.png";
        static readonly Lazy<ImageSource> s_icon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        public AppEngineRootViewModel()
        {
            Content = "AppEngine";
            Icon = s_icon.Value;
        }
    }
}
