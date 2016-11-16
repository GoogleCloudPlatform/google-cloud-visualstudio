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

using System.IO;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class represetns a .NET Core project.
    /// </summary>
    internal class NetCoreProject : ISolutionProject
    {
        private readonly string _projectJsonPath;

        #region ISolutionProject

        public string DirectoryPath => Path.GetDirectoryName(_projectJsonPath);

        public string FullPath => _projectJsonPath;

        public string Name => Path.GetFileName(Path.GetDirectoryName(_projectJsonPath));

        public KnownProjectTypes ProjectType => KnownProjectTypes.NetCoreWebApplication;

        #endregion

        public NetCoreProject(string projectJsonPath)
        {
            _projectJsonPath = projectJsonPath;
        }
    }
}
