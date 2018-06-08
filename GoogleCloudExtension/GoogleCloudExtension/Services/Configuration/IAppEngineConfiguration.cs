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
using GoogleCloudExtension.Projects;

namespace GoogleCloudExtension.Services.Configuration
{
    public interface IAppEngineConfiguration
    {
        /// <summary>
        /// Generates the app.yaml for the given project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="service">The name of the service the new app.yaml should target, if any.</param>
        void GenerateAppYaml(IParsedProject project, string service = AppEngineConfiguration.DefaultServiceName);

        /// <summary>
        /// Checks the project configuration files to see if they exist.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>An instance of <seealso cref="ProjectConfigurationStatus"/> with the status of the config.</returns>
        ProjectConfigurationStatus CheckProjectConfiguration(IParsedProject project);

        /// <summary>
        /// This methods looks for lines of the form "service: name" in the app.yaml file provided.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The service name if found, <see cref="AppEngineConfiguration.DefaultServiceName"/> if not found.</returns>
        string GetAppEngineService(IParsedProject project);

        /// <summary>
        /// Updates or creates the app.yaml for the project to target the given service.
        /// </summary>
        /// <param name="project">The project to update the app.yaml of.</param>
        /// <param name="service">The name of the service to target.</param>
        void SaveServiceToAppYaml(IParsedDteProject project, string service);

        void CopyOrCreateAppYaml(IParsedProject project, string targetDirectory, string service);

        string GetAppEngineRuntime(IParsedProject project);
    }
}