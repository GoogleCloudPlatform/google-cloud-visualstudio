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
using GoogleCloudExtension.Services.FileSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using YamlDotNet.Serialization;

namespace GoogleCloudExtension.Services.Configuration
{

    [Export(typeof(IAppEngineConfiguration))]
    public class AppEngineConfiguration : IAppEngineConfiguration
    {
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly Deserializer _yamlDeserializer = new Deserializer();
        private readonly Serializer _yamlSerializer = new Serializer();
        public const string AppYamlName = "app.yaml";
        public const string DockerfileName = NetCoreAppUtils.DockerfileName;

        public const string AppYamlDefaultContent =
            "runtime: aspnetcore\n" +
            "env: flex\n";

        public const string AppYamlServiceSpecificContentFormat =
            "runtime: aspnetcore\n" +
            "env: flex\n" +
            "service: {0}\n";

        public const string DefaultServiceName = "default";
        private const string ServiceYamlProperty = "service";
        private const string RuntimeYamlProperty = "runtime";
        public const string AspNetCoreRuntime = "aspnetcore";
        public const string CustomRuntime = "custom";

        public AppEngineConfiguration(Lazy<IFileSystem> fileSystem)
        {
            _fileSystem = fileSystem;
        }

        private IFileSystem FileSystem => _fileSystem.Value;

        /// <summary>
        /// Updates or generates tha app.yaml file for the given project with the given servie.
        /// </summary>
        /// <param name="project">The project that contains the app.yaml</param>
        /// <param name="service">The name of the service the app.yaml will target.</param>
        public void SaveServiceToAppYaml(IParsedDteProject project, string service)
        {
            string appYamlPath = GetAppYamlPath(project);
            if (FileSystem.File.Exists(appYamlPath))
            {
                SetAppYamlService(service, appYamlPath);
            }
            else
            {
                GenerateAppYaml(project, service);
            }
        }

        private void SetAppYamlService(string service, string sourceAppYaml, string targetAppYaml = null)
        {
            targetAppYaml = targetAppYaml ?? sourceAppYaml;
            Dictionary<object, object> yamlObject;
            using (TextReader reader = FileSystem.File.OpenText(sourceAppYaml))
            {
                yamlObject = _yamlDeserializer.Deserialize<Dictionary<object, object>>(reader);
            }

            if (!IsDefaultService(service))
            {
                yamlObject[ServiceYamlProperty] = service;
            }
            else if (yamlObject.ContainsKey(ServiceYamlProperty))
            {
                yamlObject.Remove(ServiceYamlProperty);
            }

            using (TextWriter writer = FileSystem.File.CreateText(targetAppYaml))
            {
                _yamlSerializer.Serialize(writer, yamlObject);
            }
        }

        /// <summary>
        /// Generates the app.yaml for the given project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="service">The service the new app.yaml will target. Defaults to the default service.</param>
        public void GenerateAppYaml(IParsedProject project, string service = DefaultServiceName)
        {
            string targetAppYaml = GetAppYamlPath(project);
            if (IsDefaultService(service))
            {
                FileSystem.File.WriteAllText(targetAppYaml, AppYamlDefaultContent);
            }
            else
            {
                FileSystem.File.WriteAllText(
                    targetAppYaml, string.Format(AppYamlServiceSpecificContentFormat, service));
            }
        }

        /// <summary>
        /// Checks the project configuration files to see if they exist.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>An instance of <seealso cref="ProjectConfigurationStatus"/> with the status of the config.</returns>
        public ProjectConfigurationStatus CheckProjectConfiguration(IParsedProject project)
        {
            string projectDirectory = project.DirectoryPath;
            string targetAppYaml = Path.Combine(projectDirectory, AppYamlName);
            bool hasAppYaml = FileSystem.File.Exists(targetAppYaml);
            bool hasDockefile = NetCoreAppUtils.CheckDockerfile(project);

            return new ProjectConfigurationStatus(hasAppYaml: hasAppYaml, hasDockerfile: hasDockefile);
        }

        /// <summary>
        /// This methods looks for lines of the form "service: name" in the app.yaml file provided.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The service name if found, <seealso cref="DefaultServiceName"/> if not found.</returns>
        public string GetAppEngineService(IParsedProject project)
        {
            string appYamlPath = GetAppYamlPath(project);
            return GetYamlProperty(appYamlPath, ServiceYamlProperty, DefaultServiceName);
        }


        /// <summary>
        /// Gets the runtime defined in the app.yaml, or <see cref="AspNetCoreRuntime"/> if there is no app.yaml.
        /// </summary>
        /// <param name="project">The project that contains the app.yaml.</param>
        /// <returns>The runtime in the app.yaml, or <see cref="AspNetCoreRuntime"/> if there is no app.yaml</returns>
        public string GetAppEngineRuntime(IParsedProject project)
        {
            string appYaml = GetAppYamlPath(project);
            if (FileSystem.File.Exists(appYaml))
            {
                return GetYamlProperty(appYaml, RuntimeYamlProperty);
            }
            else
            {
                return AspNetCoreRuntime;
            }
        }

        private string GetAppYamlPath(IParsedProject project) => Path.Combine(project.DirectoryPath, AppYamlName);

        private string GetYamlProperty(string yamlPath, string property, string defaultValue = null)
        {
            if (FileSystem.File.Exists(yamlPath))
            {
                Dictionary<object, object> yamlObject;
                using (TextReader reader = FileSystem.File.OpenText(yamlPath))
                {
                    yamlObject = _yamlDeserializer.Deserialize<Dictionary<object, object>>(reader);
                }

                if (yamlObject != null && yamlObject.ContainsKey(property))
                {
                    return yamlObject[property].ToString();
                }
                else
                {
                    return defaultValue;
                }
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Creates an app.yaml in the target directory that targets the given service.
        /// If the given project contains an app.yaml, it is the source of the new file.
        /// Otherwise, a defualt app.yaml is created.
        /// </summary>
        /// <param name="project">The project that may have a source app.yaml to copy.</param>
        /// <param name="targetDirectory">The directory to create or copy the app.yaml to.</param>
        /// <param name="service">The service the new app.yaml will target.</param>
        public void CopyOrCreateAppYaml(IParsedProject project, string targetDirectory, string service)
        {
            string sourceAppYaml = GetAppYamlPath(project);
            string targetAppYaml = Path.Combine(targetDirectory, AppYamlName);

            if (FileSystem.File.Exists(sourceAppYaml))
            {
                string appYamlService = GetAppEngineService(project);
                if (service == appYamlService ||
                    IsDefaultService(service) && IsDefaultService(appYamlService))
                {
                    FileSystem.File.Copy(sourceAppYaml, targetAppYaml, true);
                }
                else
                {
                    SetAppYamlService(service, sourceAppYaml, targetAppYaml);
                }
            }
            else
            {
                if (IsDefaultService(service))
                {
                    FileSystem.File.WriteAllText(targetAppYaml, AppYamlDefaultContent);
                }
                else
                {
                    FileSystem.File.WriteAllText(
                        targetAppYaml, string.Format(AppYamlServiceSpecificContentFormat, service));
                }
            }
        }

        private static bool IsDefaultService(string service)
        {
            return string.IsNullOrWhiteSpace(service) || service == DefaultServiceName;
        }
    }
}