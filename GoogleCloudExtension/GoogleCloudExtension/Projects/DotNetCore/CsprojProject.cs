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
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Utils;
using System.IO;

namespace GoogleCloudExtension.Projects.DotNetCore
{
    /// <summary>
    /// This class represents a .NET Core project based on .csproj.
    /// </summary>
    public class CsprojProject : IParsedDteProject
    {
        public const string NetCoreApp1_0 = "netcoreapp1.0";
        public const string NetCoreApp1_1 = "netcoreapp1.1";
        public const string NetCoreApp2_0 = "netcoreapp2.0";
        public Project Project { get; }

        public string DirectoryPath => Path.GetDirectoryName(Project.FullName);

        public string FullPath => Project.FullName;

        public string Name => Project.Name;

        public KnownProjectTypes ProjectType { get; }

        public CsprojProject(Project project, string targetFramework)
        {
            GcpOutputWindow.OutputDebugLine($"Found project {project.FullName} targeting {targetFramework}");

            Project = project;
            switch (targetFramework)
            {
                case NetCoreApp1_0:
                    ProjectType = KnownProjectTypes.NetCoreWebApplication1_0;
                    break;

                case NetCoreApp1_1:
                    ProjectType = KnownProjectTypes.NetCoreWebApplication1_1;
                    break;

                case NetCoreApp2_0:
                    ProjectType = KnownProjectTypes.NetCoreWebApplication2_0;
                    break;

                default:
                    GcpOutputWindow.OutputDebugLine($"Unsupported target framework {targetFramework}");
                    ProjectType = KnownProjectTypes.None;
                    break;
            }
        }
    }
}
