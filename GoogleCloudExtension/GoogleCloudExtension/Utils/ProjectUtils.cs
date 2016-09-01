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

using EnvDTE;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GoogleCloudExtension.Utils
{
    internal enum KnownProjectTypes
    {
        None,
        WebApplication,
        NetCoreWebApplication,
    }

    internal static class ProjectUtils
    {
        private const string MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private const string WebApplicationGuid = "{349c5851-65df-11da-9384-00065b846f21}";

        public static KnownProjectTypes GetProjectType(this Project project)
        {
            var projectFullPath = project.FullName;

            if (Path.GetExtension(projectFullPath) == ".xproj")
            {
                return KnownProjectTypes.NetCoreWebApplication;
            }
            else
            {
                var dom = XDocument.Load(project.FullName);
                var projectGuids = dom.Root
                    .Elements(XName.Get("PropertyGroup", MsbuildNamespace))
                    .Descendants(XName.Get("ProjectTypeGuids", MsbuildNamespace))
                    .Select(x => x.Value)
                    .FirstOrDefault();

                if (projectGuids == null)
                {
                    return KnownProjectTypes.None;
                }

                var guids = projectGuids.Split(';');
                if (guids.Contains(WebApplicationGuid))
                {
                    return KnownProjectTypes.WebApplication;
                }
                return KnownProjectTypes.None;
            }
        }
    }
}
