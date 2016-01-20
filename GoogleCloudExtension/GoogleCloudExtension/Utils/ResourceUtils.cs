// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Diagnostics;
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
