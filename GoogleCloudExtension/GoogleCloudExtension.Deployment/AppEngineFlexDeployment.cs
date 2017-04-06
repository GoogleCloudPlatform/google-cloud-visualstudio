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

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class implements all of the necessary details to deploy an ASP.NET Core application
    /// to the App Engine Flex environment.
    /// </summary>
    public static class AppEngineFlexDeployment
    {
        public const string AppYamlName = "app.yaml";
        public const string DockerfileName = NetCoreAppUtils.DockerfileName;

        private const string AppYamlDefaultContent =
            "runtime: aspnetcore\n" +
            "env: flex\n";

        private const string DefaultServiceName = "default";
        private const string ServiceYamlProperty = "service";
        private const string RuntimeYamlProperty = "runtime";

        private const string AspNetCoreRuntime = "aspnetcore";
        private const string CustomRuntime = "custom";

        /// <summary>
        /// The options for the deployment operation.
        /// </summary>
        public class DeploymentOptions
        {
            /// <summary>
            /// What version name to use when deploying. If null a default version name based on current time and
            /// date will be used.
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            /// Whether to promote the new version to receive 100% of the traffic or not.
            /// </summary>
            public bool Promote { get; set; }

            /// <summary>
            /// The context on which to execute the underlying gcloud command.
            /// </summary>
            public GCloudContext Context { get; set; }
        }

        /// <summary>
        /// Publishes the ASP.NET Core project to App Engine Flex.
        /// </summary>
        /// <param name="project">The project to deploy.</param>
        /// <param name="options">The <seealso cref="DeploymentOptions"/> to use.</param>
        /// <param name="progress">The progress indicator.</param>
        /// <param name="toolsPathProvider">The tools path provider to use.</param>
        /// <param name="outputAction">The action to call with lines from the command output.</param>
        public static async Task<AppEngineFlexDeploymentResult> PublishProjectAsync(
            IParsedProject project,
            DeploymentOptions options,
            IProgress<double> progress,
            IToolsPathProvider toolsPathProvider,
            Action<string> outputAction)
        {
            var stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);
            progress.Report(0.1);

            using (var cleanup = new Disposable(() => CommonUtils.Cleanup(stageDirectory)))
            {
                // Wait for the bundle creation operation to finish, updating progress as it goes.
                if (!await ProgressHelper.UpdateProgress(
                        NetCoreAppUtils.CreateAppBundleAsync(project, stageDirectory, toolsPathProvider, outputAction),
                        progress,
                        from: 0.1, to: 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return null;
                }

                var runtime = GetAppEngineRuntime(project);
                CopyOrCreateAppYaml(project, stageDirectory);
                if (runtime == CustomRuntime)
                {
                    Debug.WriteLine($"Copying Docker file to {stageDirectory} with custom runtime.");
                    NetCoreAppUtils.CopyOrCreateDockerfile(project, stageDirectory);
                }
                else
                {
                    Debug.WriteLine($"Detected runtime {runtime}");
                }
                progress.Report(0.4);

                // Deploy to app engine, this is where most of the time is going to be spent. Wait for
                // the operation to finish, update the progress as it goes.
                var effectiveVersion = options.Version ?? GetDefaultVersion();
                var deployTask = DeployAppBundleAsync(
                    stageDirectory: stageDirectory,
                    version: effectiveVersion,
                    promote: options.Promote,
                    context: options.Context,
                    outputAction: outputAction);
                if (!await ProgressHelper.UpdateProgress(deployTask, progress, 0.6, 0.9))
                {
                    Debug.WriteLine("Failed to deploy bundle.");
                    return null;
                }
                progress.Report(1.0);

                var service = GetAppEngineService(project);
                return new AppEngineFlexDeploymentResult(
                    projectId: options.Context.ProjectId,
                    service: service,
                    version: effectiveVersion,
                    promoted: options.Promote);
            }
        }

        /// <summary>
        /// Generates the app.yaml for the given project.
        /// </summary>
        /// <param name="project">The project.</param>
        public static bool GenerateAppYaml(IParsedProject project)
        {
            try
            {
                var targetAppYaml = Path.Combine(project.DirectoryPath, AppYamlName);
                File.WriteAllText(targetAppYaml, AppYamlDefaultContent);
                return true;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to generate app.yaml: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates the Dockerfile for the given project.
        /// </summary>
        /// <param name="project">The project.</param>
        public static bool GenerateDockerfile(IParsedProject project)
        {
            try
            {
                NetCoreAppUtils.GenerateDockerfile(project);
                return true;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to generate Dockerfile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks the project configuration files to see if they exist.
        /// </summary>
        /// <param name="projectPath">The project.</param>
        /// <returns>An instance of <seealso cref="ProjectConfigurationStatus"/> with the status of the config.</returns>
        public static ProjectConfigurationStatus CheckProjectConfiguration(IParsedProject project)
        {
            var projectDirectory = project.DirectoryPath;
            var targetAppYaml = Path.Combine(projectDirectory, AppYamlName);
            var hasAppYaml = File.Exists(targetAppYaml);
            var hasDockefile = NetCoreAppUtils.CheckDockerfile(project);

            return new ProjectConfigurationStatus(hasAppYaml: hasAppYaml, hasDockerfile: hasDockefile);
        }

        /// <summary>
        /// This methods looks for lines of the form "service: name" in the app.yaml file provided.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The service name if found, <seealso cref="DefaultServiceName"/> if not found.</returns>
        private static string GetAppEngineService(IParsedProject project)
        {
            string appYaml = GetAppYamlPath(project);
            return GetYamlProperty(yamlPath: appYaml, property: ServiceYamlProperty, defaultValue: DefaultServiceName);
        }

        private static string GetAppEngineRuntime(IParsedProject project)
        {
            string appYaml = GetAppYamlPath(project);
            if (!File.Exists(appYaml))
            {
                return AspNetCoreRuntime;
            }
            return GetYamlProperty(appYaml, RuntimeYamlProperty);
        }

        private static string GetAppYamlPath(IParsedProject project)
        {
            var appYaml = Path.Combine(project.DirectoryPath, AppYamlName);
            return appYaml;
        }

        private static string GetYamlProperty(string yamlPath, string property, string defaultValue = null)
        {
            string result = defaultValue;
            var propertyName = $"{property}:";

            if (File.Exists(yamlPath))
            {
                try
                {
                    var lines = File.ReadLines(yamlPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith(propertyName))
                        {
                            var name = line.Substring(propertyName.Length);
                            result = name.Trim();
                            break;
                        }
                    }
                }
                catch (IOException ex)
                {
                    throw new DeploymentException(ex.Message, ex);
                }
            }

            return result;
        }

        private static string GetDefaultVersion()
        {
            var now = DateTime.Now;
            return String.Format(
                "{0:0000}{1:00}{2:00}t{3:00}{4:00}{5:00}",
                now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second);
        }

        private static void CopyOrCreateAppYaml(IParsedProject project, string stageDirectory)
        {
            var sourceAppYaml = Path.Combine(project.DirectoryPath, AppYamlName);
            var targetAppYaml = Path.Combine(stageDirectory, AppYamlName);

            if (File.Exists(sourceAppYaml))
            {
                File.Copy(sourceAppYaml, targetAppYaml, overwrite: true);
            }
            else
            {
                File.WriteAllText(targetAppYaml, AppYamlDefaultContent);
            }
        }

        private static Task<bool> DeployAppBundleAsync(
            string stageDirectory,
            string version,
            bool promote,
            GCloudContext context,
            Action<string> outputAction)
        {
            var appYamlPath = Path.Combine(stageDirectory, AppYamlName);
            return GCloudWrapper.DeployAppAsync(
                appYaml: appYamlPath,
                version: version,
                promote: promote,
                outputAction: outputAction,
                context: context);
        }
    }
}
