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

using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System;
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtension.TeamExplorerExtension
{
    /// <summary>
    /// A wrapper to Microsoft.TeamFoundation.Git.Provider.dll.
    /// VS2015 and VS2017 can implement the wrapper respectively.
    /// </summary>
    public class GitExtensionWrapper
    {
        private readonly IServiceProvider _serviceProvider;

        private IGitExt GitExtension => _serviceProvider.GetService(typeof(IGitExt)) as IGitExt;

        public GitExtensionWrapper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider.ThrowIfNull(nameof(serviceProvider));
        }

        /// <summary>
        /// Returns current active git repository local root path.
        /// </summary>
        /// <returns>
        /// Currently active repository local root that is set by Visual Studio.
        /// Or null if there is no active repository.
        /// </returns>
        public string GetActiveRepository()
        {
            IGitRepositoryInfo activeRepoInfo = GitExtension?.ActiveRepositories?.FirstOrDefault();
            Debug.WriteLine($"GetActiveRepo {activeRepoInfo} {activeRepoInfo?.RepositoryPath} {activeRepoInfo?.CurrentBranch}");
            return activeRepoInfo?.RepositoryPath;
        }
    }
}
