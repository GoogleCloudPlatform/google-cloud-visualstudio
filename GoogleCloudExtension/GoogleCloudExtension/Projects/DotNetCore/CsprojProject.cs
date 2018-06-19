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
using System.IO;
using System.Text.RegularExpressions;

namespace GoogleCloudExtension.Projects.DotNetCore
{
    /// <summary>
    /// This class represents a .NET Core project based on .csproj.
    /// </summary>
    public class CsprojProject : IParsedDteProject
    {
        private static readonly Regex s_frameworkVersionRegex = new Regex(@"(?<=^netcoreapp)[\d.]+$");
        public Project Project { get; }

        public string DirectoryPath => Path.GetDirectoryName(Project.FullName);

        public string FullPath => Project.FullName;

        public string Name => Project.Name;

        public KnownProjectTypes ProjectType => KnownProjectTypes.NetCoreWebApplication;

        /// <summary>The version of the framework used by the project.</summary>
        public string FrameworkVersion { get; }

        public CsprojProject(Project project, string targetFramework)
        {
            Project = project;
            FrameworkVersion = s_frameworkVersionRegex.Match(targetFramework).Value;
        }
    }
}
