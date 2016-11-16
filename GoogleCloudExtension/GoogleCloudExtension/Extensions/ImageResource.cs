using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Media;

namespace GoogleCloudExtension.Extensions
{
    /// <summary>
    /// This class is a simple markup extension that returns an image given its path.
    /// </summary>
    public class ImageResource : MarkupExtension
    {
        // Global cache of images.
        private static readonly Dictionary<string, ImageSource> s_cache = new Dictionary<string, ImageSource>();

        /// <summary>
        /// The path to load.
        /// </summary>
        public string Path { get; set; }

        public ImageResource()
        { }

        public ImageResource(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Loads the image given the <seealso cref="Path"/> property. Caching is performed so an image is
        /// only going to be loaded once.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            ImageSource result = null;
            if (!s_cache.TryGetValue(Path, out result))
            {
                result = ResourceUtils.LoadImage(Path);
                s_cache[Path] = result;
            }

            return result;
        }
    }
}
