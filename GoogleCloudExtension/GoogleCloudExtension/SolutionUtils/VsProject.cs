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
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class represents a native Visual Studio project.
    /// </summary>
    internal class VsProject : ISolutionProject
    {
        private const string MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private const string WebApplicationGuid = "{349c5851-65df-11da-9384-00065b846f21}";

        private readonly Project _project;
        private readonly Lazy<KnownProjectTypes> _knownProjectType;

        #region ISolutionProject

        public string DirectoryPath => Path.GetDirectoryName(_project.FullName);

        public string FullPath => _project.FullName;

        public string Name => _project.Name;

        public KnownProjectTypes ProjectType => _knownProjectType.Value;

        #endregion

        public VsProject(Project project)
        {
            _project = project;
            _knownProjectType = new Lazy<KnownProjectTypes>(GetProjectType);
        }

        private KnownProjectTypes GetProjectType()
        {
            var dom = XDocument.Load(_project.FullName);
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
