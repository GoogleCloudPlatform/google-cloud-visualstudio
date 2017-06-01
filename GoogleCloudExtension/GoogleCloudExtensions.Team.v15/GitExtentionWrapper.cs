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

using System;
using System.Linq;
using static System.Diagnostics.Debug;

namespace GoogleCloudExtension.Team
{
    /// <summary>
    /// A wrapper to Microsoft.TeamFoundation.Git.Provider.dll.
    /// In VS2017, Microsoft.TeamFoundation.Git.Provider.dll is dependent on .Net 4.6.1
    /// Before we upgrade all projects to .NET 4.6.1, VS2017 version won't have this feature.
    /// </summary>
    public class GitExtentionWrapper
    {
        public GitExtentionWrapper(IServiceProvider serviceProvider)
        { }

        /// <summary>
        /// Simply returns null for now. Will support it when all projects upgrade to .NET 4.6.1
        /// </summary>
        public string GetActiveRepository() => null;
    }
}
