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
    internal class CsprojProject : IParsedProject
    {
        private readonly Project _project;

        #region IParsedProject

        public string DirectoryPath => Path.GetDirectoryName(_project.FullName);

        public string FullPath => _project.FullName;

        public string Name => _project.Name;

        public KnownProjectTypes ProjectType { get; }

        #endregion

        public CsprojProject(Project project, string targetFramework)
        {
            GcpOutputWindow.OutputDebugLine($"Found project {project.FullName} targeting {targetFramework}");

            _project = project;
            switch (targetFramework)
            {
                case "netcoreapp1.0":
                    ProjectType = KnownProjectTypes.NetCoreWebApplication1_0;
                    break;

                case "netcoreapp1.1":
                    ProjectType = KnownProjectTypes.NetCoreWebApplication1_1;
                    break;

                default:
                    GcpOutputWindow.OutputDebugLine($"Unsopported target framework {targetFramework}");
                    ProjectType = KnownProjectTypes.None;
                    break;
            }
        }
    }
}
