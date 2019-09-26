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
using System.Windows.Media;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// Define all shared images here.
    /// </summary>
    public class CommonImageResources
    {
        private const string CloudLogoIconPath = "Theming/Resources/logo_cloud.png";

        private static readonly Lazy<ImageSource> s_logoIcon = new Lazy<ImageSource>(
            () => ResourceUtils.LoadImage(CloudLogoIconPath));

        /// <summary>
        /// The 16x16 size cloud logo ImageSource.
        /// </summary>
        public static ImageSource CloudLogo16By16 => s_logoIcon.Value;
    }
}
