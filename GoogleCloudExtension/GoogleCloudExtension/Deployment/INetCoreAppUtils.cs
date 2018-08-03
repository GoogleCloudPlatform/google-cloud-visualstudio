// Copyright 2018 Google Inc. All Rights Reserved.
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
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{

    /// <summary>
    /// Service interface for manipulating .NET Core projects.
    /// </summary>
    public interface INetCoreAppUtils
    {
        /// <summary>
        /// Creates an app bundle by publishing it to the given directory.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="stageDirectory">The directory to which to publish.</param>
        /// <param name="outputAction">The callback to call with output from the command.</param>
        /// <param name="configuration">The name of the configuration to publish.</param>
        Task<bool> CreateAppBundleAsync(
            IParsedProject project,
            string stageDirectory,
            Action<string> outputAction,
            string configuration);

        /// <summary>
        /// Creates the Dockerfile necessary to package up an ASP.NET Core app if one is not already present at the root
        /// path of the project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="stageDirectory">The directory where to save the Dockerfile.</param>
        void CopyOrCreateDockerfile(IParsedProject project, string stageDirectory);

        /// <summary>
        /// Generates the Dockerfile for this .NET Core project.
        /// </summary>
        /// <param name="project">The project.</param>
        void GenerateDockerfile(IParsedProject project);

        /// <summary>
        /// Checks if the Dockerfile for the project was created.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>True if the Dockerfile exists, false otherwise.</returns>
        bool CheckDockerfile(IParsedProject project);
    }
}