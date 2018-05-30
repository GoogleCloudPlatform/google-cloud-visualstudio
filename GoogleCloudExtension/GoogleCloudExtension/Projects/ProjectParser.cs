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

using EnvDTE;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GoogleCloudExtension.Projects
{
    /// <summary>
    /// This class contains helpers to instantiate the right type of <seealso cref="IParsedProject"/> implementation
    /// depending on the project being loaded.
    /// </summary>
    internal static class ProjectParser
    {
        // Identifiers of an ASP.NET 4.x .csproj
        private const string MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private const string WebApplicationGuid = "{349c5851-65df-11da-9384-00065b846f21}";

        // Identifier of an ASP.NET Core 1.x .csproj
        private const string AspNetCoreSdk = "Microsoft.NET.Sdk.Web";

        // Extension used in .NET Core 1.0 project placeholders, the real project is in project.json.
        private const string XProjExtension = ".xproj";
        private const string ProjectJsonFileName = "project.json";

        // Extension used in .NET Core 1.0, 1.1... and .NET 4.x, etc...
        private const string CSProjExtension = ".csproj";

        // Elements and attributes to fetch from the .csproj.
        private const string PropertyGroupElementName = "PropertyGroup";
        private const string TargetFrameworkElementName = "TargetFramework";

        // The PropertyTypeGuids element contains the GUID that identifies the type of project, console, web, etc...
        private const string PropertyTypeGuidsElementName = "ProjectTypeGuids";

        // The Sdk attribute points to the SDK used to build this app, console or web.
        private const string SdkAttributeName = "Sdk";

        /// <summary>
        /// Parses the given <seealso cref="Project"/> instance and resturns a friendlier and more usable type to use for
        /// deployment and other operations.
        /// </summary>
        /// <param name="project">The <seealso cref="Project"/> instance to parse.</param>
        /// <returns>The resulting <seealso cref="IParsedProject"/> or null if the project is not supported.</returns>
        public static IParsedDteProject ParseProject(Project project)
        {
            var extension = Path.GetExtension(project.FullName);
            switch (extension)
            {
                case XProjExtension:
                    Debug.WriteLine($"Processing a project.json: {project.FullName}");
                    return ParseJsonProject(project);

                case CSProjExtension:
                    Debug.WriteLine($"Processing a .csproj: {project.FullName}");
                    return ParseCsprojProject(project);

                default:
                    return null;
            }
        }

        private static IParsedDteProject ParseCsprojProject(Project project)
        {
            GcpOutputWindow.OutputDebugLine($"Parsing .csproj {project.FullName}");

            try
            {
                var dom = XDocument.Load(project.FullName);
                var sdk = dom.Root.Attribute(SdkAttributeName);
                if (sdk != null)
                {
                    GcpOutputWindow.OutputDebugLine($"Found a .NET Core style .csproj {sdk.Value}");
                    if (sdk.Value == AspNetCoreSdk)
                    {
                        var targetFramework = dom.Root
                            .Elements(PropertyGroupElementName)
                            .Descendants(TargetFrameworkElementName)
                            .Select(x => x.Value)
                            .FirstOrDefault();
                        return new DotNetCore.CsprojProject(project, targetFramework);
                    }
                }

                var projectGuids = dom.Root
                    .Elements(XName.Get(PropertyGroupElementName, MsbuildNamespace))
                    .Descendants(XName.Get(PropertyTypeGuidsElementName, MsbuildNamespace))
                    .Select(x => x.Value)
                    .FirstOrDefault();

                if (projectGuids == null)
                {
                    return null;
                }

                var guids = projectGuids.Split(';');
                if (guids.Contains(WebApplicationGuid))
                {
                    return new DotNet4.CsprojProject(project);
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputDebugLine($"Invalid project file {project.FullName}");
            }
            return null;
        }

        private static IParsedDteProject ParseJsonProject(Project project)
        {
            var projectDir = Path.GetDirectoryName(project.FullName);
            var projectJsonPath = Path.Combine(projectDir, ProjectJsonFileName);

            if (!File.Exists(projectJsonPath))
            {
                Debug.WriteLine($"Could not find {projectJsonPath}.");
                return null;
            }

            return new DotNetCore.JsonProject(project);
        }
    }
}
