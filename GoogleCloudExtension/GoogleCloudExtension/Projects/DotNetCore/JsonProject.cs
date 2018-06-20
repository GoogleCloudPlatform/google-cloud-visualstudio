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
using GoogleCloudExtension.Deployment;
using System.IO;

namespace GoogleCloudExtension.Projects.DotNetCore
{
    /// <summary>
    /// This class represents a project.json based .NET Core project.
    /// </summary>
    internal class JsonProject : IParsedDteProject
    {
        /// <summary>
        /// The Version of project.json style projects.
        /// </summary>
        public const string PreviewVersion = "1.0.0-preview";

        public Project Project { get; }

        public string DirectoryPath => Path.GetDirectoryName(FullPath);

        public string FullPath => Project.FullName;

        public string Name => Path.GetFileName(Path.GetDirectoryName(FullPath));

        public KnownProjectTypes ProjectType => KnownProjectTypes.NetCoreWebApplication;

        /// <summary>The version of the framework used by the project.</summary>
        public string FrameworkVersion { get; } = PreviewVersion;

        public JsonProject(Project project)
        {
            Project = project;
        }
    }
}
