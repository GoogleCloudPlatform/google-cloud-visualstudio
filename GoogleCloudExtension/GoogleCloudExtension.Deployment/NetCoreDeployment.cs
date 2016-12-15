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
    public static class NetCoreDeployment
    {
        private const string AppYamlName = "app.yaml";

        private const string AppYamlDefaultContent =
            "runtime: custom\n" +
            "vm: true\n";

        private const string DockerfileName = "Dockerfile";

        /// <summary>
        /// This template is the smallest possible Dockerfile needed to deploy an ASP.NET Core app to
        /// App Engine Flex environment. It invokes the entry point .dll given by {0}, sets up the environment
        /// so the app listens on port 8080.
        /// All of the files composing the app are copied to the /app path, then it is set as the working directory.
        /// </summary>
        private const string DockerfileDefaultContent =
            "FROM b.gcr.io/aspnet-docker/aspnet:1.0.3\n" +
            "COPY . /app\n" +
            "WORKDIR /app\n" +
            "EXPOSE 8080\n" +
            "ENV ASPNETCORE_URLS=http://*:8080\n" +
            "ENTRYPOINT [\"dotnet\", \"{0}.dll\"]\n";

        private const string DefaultServiceName = "default";

        private static readonly Lazy<string> s_dotnetPath = new Lazy<string>(GetDotnetPath);

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
            public Context Context { get; set; }
        }

        /// <summary>
        /// Publishes the ASP.NET Core project to App Engine Flex.
        /// </summary>
        /// <param name="projectPath">The full path to the project.json for the ASP.NET Core project.</param>
        /// <param name="options">The <seealso cref="DeploymentOptions"/> to use.</param>
        /// <param name="progress">The progress indicator.</param>
        /// <param name="outputAction">The action to call with lines from the command output.</param>
        /// <returns></returns>
        public static async Task<NetCorePublishResult> PublishProjectAsync(
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
                        CreateAppBundleAsync(projectPath, stageDirectory, outputAction),
                        progress,
                        from: 0.1, to: 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return null;
                }

                CopyOrCreateDockerfile(projectPath, stageDirectory);
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
                return new NetCorePublishResult(
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

        private static Task<bool> CreateAppBundleAsync(string projectPath, string stageDirectory, Action<string> outputAction)
        {
            var arguments = $"publish \"{projectPath}\" " +
                $"-o \"{stageDirectory}\" " +
                "-c Release";
            var externalTools = GetExternalToolsPath();
            var env = new Dictionary<string, string>
            {
                { "PATH", $"{Environment.GetEnvironmentVariable("PATH")};{externalTools}" },
            };

            Debug.WriteLine($"Using tools from {externalTools}");
            outputAction($"dotnet {arguments}");
            return ProcessUtils.RunCommandAsync(s_dotnetPath.Value, arguments, (o, e) => outputAction(e.Line), env);
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

        private static void CopyOrCreateDockerfile(string projectPath, string stageDirectory)
        {
            var sourceDir = Path.GetDirectoryName(projectPath);
            var sourceDockerfile = Path.Combine(sourceDir, DockerfileName);
            var targetDockerfile = Path.Combine(stageDirectory, DockerfileName);

            if (File.Exists(sourceDockerfile))
            {
                File.Copy(sourceDockerfile, targetDockerfile, overwrite: true);
            }
            else
            {
                var content = String.Format(DockerfileDefaultContent, GetProjectName(projectPath));
                File.WriteAllText(targetDockerfile, content);
            }
        }

        private static Task<bool> DeployAppBundleAsync(
            string stageDirectory,
            string version,
            bool promote,
            Context context,
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

        private static string GetDotnetPath()
        {
            var programFilesPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            return Path.Combine(programFilesPath, @"dotnet\dotnet.exe");
        }

        private static string GetProjectName(string projectPath)
        {
            var directory = Path.GetDirectoryName(projectPath);
            return Path.GetFileName(directory);
        }

        private static string GetExternalToolsPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            return Path.Combine(programFilesPath, @"Microsoft Visual Studio 14.0\Web\External");
        }
    }
}
