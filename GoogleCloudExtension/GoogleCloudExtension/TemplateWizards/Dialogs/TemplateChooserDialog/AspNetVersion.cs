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
using System.Linq;

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

        private static readonly Version s_sdkVersion1_0 = new Version(1, 0);
        private static readonly Version s_sdkVersion2_0 = new Version(2, 0);

        /// <summary>
        /// The version number of ASP.NET. This corresponds to the version of .NET Core used as well.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Whether this is ASP.NET or ASP.NET Core
        /// </summary>
        public bool IsCore { get; }

        /// <summary>
        /// Gets the installed .NET Core SDK versions.
        /// </summary>
        private static IEnumerable<string> NetCoreSdkVersions =>
            VsVersionUtils.ToolsPathProvider.GetNetCoreSdkVersions();

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
            if (NetCoreSdkVersions.Any(sdkVersion => sdkVersion.StartsWith("1.0.0-preview")))
            {
                return new List<AspNetVersion>
                {
                    AspNetCore1Preview
                };
            }
            else
            {
                return new List<AspNetVersion>();
            }
        }

        /// <summary>
        /// ASP.NET Core versions available to VS 2017.
        /// </summary>
        private static IList<AspNetVersion> GetVs2017AspNetCoreVersions()
        {
            List<Version> sdkVersions = GetParsedSdkVersions().ToList();
            var aspNetVersions = new List<AspNetVersion>();
            if (sdkVersions.Any(version => version >= s_sdkVersion1_0 && version < s_sdkVersion2_0))
            {
                aspNetVersions.Add(AspNetCore10);
                aspNetVersions.Add(AspNetCore11);
            }
            if (sdkVersions.Any(version => version >= s_sdkVersion2_0))
            {
                aspNetVersions.Add(AspNetCore20);
            }
            return aspNetVersions;
        }

        private static IEnumerable<Version> GetParsedSdkVersions()
        {
            foreach (string sdkVersion in NetCoreSdkVersions)
            {
                Version result;
                if (System.Version.TryParse(sdkVersion, out result))
                {
                    yield return result;
                }
            }
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
        /// <param name="framework">The <see cref="FrameworkType"/> that will run this template.</param>
        /// <returns>A new list of AspNetVersions from which are compatible with the vsVersion and framework.</returns>
        public static IList<AspNetVersion> GetAvailableVersions(FrameworkType framework)
        {
            switch (framework)
            {
                case FrameworkType.NetFramework:
                    return new List<AspNetVersion> { AspNet4 };
                case FrameworkType.NetCore:
                    switch (GoogleCloudExtensionPackage.VsVersion)
                    {
                        case VsVersionUtils.VisualStudio2015Version:
                            return GetVs2015AspNetCoreVersions();
                        default:
                            // For forward compatibility, give future versions of VS the same options as VS2017.
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
