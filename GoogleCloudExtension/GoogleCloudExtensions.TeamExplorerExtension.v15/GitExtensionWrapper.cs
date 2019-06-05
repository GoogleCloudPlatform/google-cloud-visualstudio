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

namespace GoogleCloudExtension.TeamExplorerExtension
{
    /// <summary>
    /// A wrapper to Microsoft.TeamFoundation.Git.Provider.dll.
    /// The <seealso cref="TeamExplorerUtils"/> class depends on methods exposed by the dll. 
    /// In VS2015, we can use the interface. 
    /// In VS2017, Microsoft.TeamFoundation.Git.Provider.dll is dependent on .Net 4.6.1
    /// Before we upgrade all projects to .NET 4.6.1, in VS2017 version, 
    /// we won't be able to use the interface of Microsoft.TeamFoundation.Git.Provider.dll
    /// 
    /// We add this empty class here so that the <seealso cref="TeamExplorerUtils"/> class can compile
    /// for both VS2015 and VS2017 versions. 
    /// And once we upgrade to .NET 4.6.1, we'll be able to add real code to make VS2017 version work too.
    /// </summary>
    public class GitExtensionWrapper
    {
        public GitExtensionWrapper(IServiceProvider serviceProvider)
        { }

        /// <summary>
        /// Simply returns null for now. Will support it when all projects upgrade to .NET 4.6.1
        /// </summary>
        public string GetActiveRepository() => null;
    }
}
