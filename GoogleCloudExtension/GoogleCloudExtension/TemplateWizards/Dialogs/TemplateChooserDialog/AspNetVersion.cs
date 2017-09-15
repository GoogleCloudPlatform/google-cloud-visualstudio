// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.VsVersion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// Describes an ASP.NET version.
    /// </summary>
    public class AspNetVersion
    {
        /// <summary>
        /// The preview version of ASP.NET Core used by VS 2015.
        /// </summary>
        public static readonly AspNetVersion AspNetCore1Preview = new AspNetVersion("1.0-preview");

        /// <summary>
        /// ASP.NET Core 1.0.
        /// </summary>
        public static readonly AspNetVersion AspNetCore10 = new AspNetVersion("1.0");

        /// <summary>
        /// ASP.NET Core 1.0.
        /// </summary>
        public static readonly AspNetVersion AspNetCore11 = new AspNetVersion("1.1");

        /// <summary>
        /// ASP.NET Core 2.0.
        /// </summary>
        public static readonly AspNetVersion AspNetCore20 = new AspNetVersion("2.0");

        /// <summary>
        /// ASP.NET 4.
        /// </summary>
        public static readonly AspNetVersion AspNet4 = new AspNetVersion("4", false);

        /// <summary>
        /// The version number of ASP.NET. This corresponds to the version of .NET Core used as well.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Whether this is ASP.NET or ASP.NET Core
        /// </summary>
        private bool IsCore { get; }

        [JsonConstructor]
        private AspNetVersion(string version, bool isCore = true)
        {
            Version = version;
            IsCore = isCore;
        }

        /// <summary>
        /// ASP.NET Core versions available to VS 2015.
        /// </summary>
        private static IList<AspNetVersion> GetVs2015AspNetCoreVersions()
        {
            return new List<AspNetVersion>
            {
                AspNetCore1Preview
            };
        }

        /// <summary>
        /// ASP.NET Core versions available to VS 2017.
        /// </summary>
        private static IList<AspNetVersion> GetVs2017AspNetCoreVersions()
        {
            return new List<AspNetVersion>
            {
                AspNetCore10,
                AspNetCore11,
                AspNetCore20
            };
        }

        /// <summary>
        /// Human readable version of this object.
        /// </summary>
        /// <returns>A localized formatted version of this ASP.NET version</returns>
        public override string ToString()
        {
            string nameFormat = IsCore ? Resources.AspNetCoreVersionedName : Resources.AspNetVersionedName;
            return string.Format(nameFormat, Version);
        }

        /// <summary>
        /// Gets the versions available for specified version of Visual Studio and Framework type.
        /// </summary>
        /// <param name="vsVersion">The string visual studio version.</param>
        /// <param name="framework">The <see cref="FrameworkType"/> that will run this template.</param>
        /// <returns>A new list of AspNetVersions from which are compatible with the vsVersion and framework.</returns>
        public static IList<AspNetVersion> GetAvailableVersions(string vsVersion, FrameworkType framework)
        {
            switch (framework)
            {
                case FrameworkType.NetFramework:
                    return new List<AspNetVersion> {AspNet4};
                case FrameworkType.NetCore:
                    switch (vsVersion)
                    {
                        case VsVersionUtils.VisualStudio2015Version:
                            return GetVs2015AspNetCoreVersions();
                        // ReSharper disable once RedundantCaseLabel
                        case VsVersionUtils.VisualStudio2017Version:
                        // For forward compatibility, give future versions of VS the same options as VS2017.
                        default:
                            return GetVs2017AspNetCoreVersions();
                    }
                case FrameworkType.None:
                    return new List<AspNetVersion>();
                default:
                    throw new InvalidOperationException($"Unknown Famework type: {framework}");
            }
        }

    }
}
