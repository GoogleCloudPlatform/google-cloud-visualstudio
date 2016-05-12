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

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers to access resources in the assembly.
    /// </summary>
    internal static class ResourceUtils
    {
        private static readonly Lazy<string> s_assemblyName = new Lazy<string>(GetAssemblyName);

        /// <summary>
        /// Loads an image resource given its relative path in the resources.
        /// </summary>
        /// <param name="path">The path of the resource to load.</param>
        /// <returns></returns>
        public static ImageSource LoadImage(string path)
        {
            var uri = new Uri($"pack://application:,,,/{s_assemblyName.Value};component/{path}");
            Debug.WriteLine($"Loading resource: {path}");
            return new BitmapImage(uri);
        }

        private static string GetAssemblyName() => Assembly.GetExecutingAssembly().GetName().Name;
    }
}
