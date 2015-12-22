using GoogleCloudExtension.CloudExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : TreeHierarchy
    {
        static readonly Lazy<BitmapImage> s_icon = new Lazy<BitmapImage>(LoadIcon);

        private static BitmapImage LoadIcon()
        {
            var uri = new Uri("pack://application:,,,/GoogleCloudExtension;component/CloudExplorerSources/AppEngine/Resources/app_engine.png");
            return new BitmapImage(uri);
        }

        public AppEngineRootViewModel()
        {
            Content = "AppEngine";
            Icon = s_icon.Value;
        }
    }
}
