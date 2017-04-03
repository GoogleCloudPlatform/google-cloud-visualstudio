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

using GoogleCloudExtension.Deployment;
using System.IO;

namespace GoogleCloudExtension.Projects
{
    /// <summary>
    /// This class represetns a project.json based .NET Core project.
    /// </summary>
    internal class NetCoreJsonProject : IParsedProject
    {
        private readonly string _projectJsonPath;

        #region IParsedProject

        public string DirectoryPath => Path.GetDirectoryName(_projectJsonPath);

        public string FullPath => _projectJsonPath;

        public string Name => Path.GetFileName(Path.GetDirectoryName(_projectJsonPath));

        public KnownProjectTypes ProjectType => KnownProjectTypes.NetCoreWebApplication1_0;

        #endregion

        public NetCoreJsonProject(string projectJsonPath)
        {
            _projectJsonPath = projectJsonPath;
        }
    }
}
