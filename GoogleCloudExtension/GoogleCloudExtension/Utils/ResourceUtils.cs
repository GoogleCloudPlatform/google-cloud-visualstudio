using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.Utils
{
    public static class ResourceUtils
    {
        private const string AssemblyName = "GoogleCloudExtension";

        public static ImageSource LoadResource(string path)
        {
            var uri = new Uri($"pack://application:,,,/{AssemblyName};component/{path}");
            Debug.WriteLine($"Loading resource: {path}");
            return new BitmapImage(uri);
        }
    }
}
