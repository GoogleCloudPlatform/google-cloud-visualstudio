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
using System.Collections.Generic;
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
        private const string AppYamlName = "app.yaml";

        private const string AppYamlDefaultContent =
            "runtime: custom\n" +
            "env: flex\n";

        private const string DefaultServiceName = "default";

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
        /// <param name="projectPath">The full path to the project.json for the ASP.NET Core project.</param>
        /// <param name="options">The <seealso cref="DeploymentOptions"/> to use.</param>
        /// <param name="progress">The progress indicator.</param>
        /// <param name="outputAction">The action to call with lines from the command output.</param>
        /// <returns></returns>
        public static async Task<AppEngineFlexDeploymentResult> PublishProjectAsync(
            string projectPath,
            DeploymentOptions options,
            IProgress<double> progress,
            Action<string> outputAction)
        {
            if (!File.Exists(projectPath))
            {
                Debug.WriteLine($"Cannot find {projectPath}, not a valid project.");
                return null;
            }

            var stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);
            progress.Report(0.1);

            using (var cleanup = new Disposable(() => Cleanup(stageDirectory)))
            {
                // Wait for the bundle creation operation to finish, updating progress as it goes.
                if (!await ProgressHelper.UpdateProgress(
                        NetCoreAppUtils.CreateAppBundleAsync(projectPath, stageDirectory, outputAction),
                        progress,
                        from: 0.1, to: 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return null;
                }

                NetCoreAppUtils.CopyOrCreateDockerfile(projectPath, stageDirectory);
                CopyOrCreateAppYaml(projectPath, stageDirectory);
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

                var service = GetAppEngineService(projectPath);
                return new AppEngineFlexDeploymentResult(
                    projectId: options.Context.ProjectId,
                    service: service,
                    version: effectiveVersion,
                    promoted: options.Promote);
            }
        }

        private static void Cleanup(string stageDirectory)
        {
            try
            {
                if (Directory.Exists(stageDirectory))
                {
                    Directory.Delete(stageDirectory, recursive: true);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to cleanup: {ex.Message}");
            }
        }

        private static string GetAppEngineService(string projectPath)
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            var appYaml = Path.Combine(projectDirectory, AppYamlName);
            if (!File.Exists(appYaml))
            {
                return DefaultServiceName;
            }
            else
            {
                // TODO: Load the app yaml and look for the service key.
                return DefaultServiceName;
            }
        }

        private static string GetDefaultVersion()
        {
            var now = DateTime.Now;
            return String.Format(
                "{0:0000}{1:00}{2:00}t{3:00}{4:00}{5:00}",
                now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second);
        }

        private static void CopyOrCreateAppYaml(string projectPath, string stageDirectory)
        {
            var sourceDir = Path.GetDirectoryName(projectPath);
            var sourceAppYaml = Path.Combine(sourceDir, AppYamlName);
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
