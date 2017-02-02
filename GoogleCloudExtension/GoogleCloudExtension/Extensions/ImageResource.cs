// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
