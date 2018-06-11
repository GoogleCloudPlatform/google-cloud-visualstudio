﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.Projects.DotNet4
{
    /// <summary>
    /// This class represents .NET 4.x .csproj based project.
    /// </summary>
    internal class CsprojProject : IParsedProject
    {
        private static readonly Regex s_frameworkVersionRegex = new Regex("(?<=Version=v)[\\d.]+");
        private readonly Project _project;

        #region IParsedProject

        public string DirectoryPath => Path.GetDirectoryName(_project.FullName);

        public string FullPath => _project.FullName;

        public string Name => _project.Name;

        public KnownProjectTypes ProjectType => KnownProjectTypes.WebApplication;

        /// <summary>The version of the framework used by the project.</summary>
        public string FrameworkVersion { get; }

        #endregion

        public CsprojProject(Project project)
        {
            _project = project;
            string targetFramework = project.Properties.Item("TargetFrameworkMoniker").Value.ToString();
            FrameworkVersion = s_frameworkVersionRegex.Match(targetFramework).Value;
        }
    }
}