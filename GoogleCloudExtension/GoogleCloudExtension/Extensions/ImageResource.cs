using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace GoogleCloudExtension.Extensions
{
    public class ImageResource : MarkupExtension
    {
        public string Path { get; set; }

        public ImageResource()
        { }

        public ImageResource(string path)
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return ResourceUtils.LoadImage(Path);
        }
    }
}
