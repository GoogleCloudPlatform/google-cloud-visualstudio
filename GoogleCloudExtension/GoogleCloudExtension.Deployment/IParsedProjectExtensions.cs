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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// Useful checks for projects.
    /// </summary>
    public static class IParsedProjectExtensions
    {
        /// <summary>
        /// Determines if the given project is a .NET Core project or not.
        /// </summary>
        /// <param name="project">The project to check.</param>
        /// <returns>Returns true if the project is a .NET Core project, false otherwise.</returns>
        public static bool IsAspNetCoreProject(this IParsedProject project)
            => project.ProjectType == KnownProjectTypes.NetCoreWebApplication1_0 ||
               project.ProjectType == KnownProjectTypes.NetCoreWebApplication1_1 ||
               project.ProjectType == KnownProjectTypes.NetCoreWebApplication2_0;
    }
}
