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
    public static class NetCoreDeployment
    {
        private const string AppYamlName = "app.yaml";

        private const string AppYamlDefaultContent =
            "runtime: custom\n" +
            "vm: true\n";

        private const string DockerfileName = "Dockerfile";

        private const string DockerfileDefaultContent =
            "FROM microsoft/dotnet:1.0.0-core\n" +
            "COPY . /app\n" +
            "WORKDIR /app\n" +
            "EXPOSE 8080\n" +
            "ENV ASPNETCORE_URLS=http://*:8080\n" +
            "ENTRYPOINT [\"dotnet\", \"{0}.dll\"]\n";

        private static readonly Lazy<string> s_dotnetPath = new Lazy<string>(GetDotnetPath);

        public class DeploymentOptions
        {
            public string Version { get; set; }

            public bool Promote { get; set; }

            public Context Context { get; set; }
        }

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

            if (!await CreateAppBundleAsync(projectPath, stageDirectory, outputAction))
            {
                Debug.WriteLine("Failed to create app bundle.");
                return null;
            }
            progress.Report(0.5);

            CopyOrCreateDockerfile(projectPath, stageDirectory);
            CopyOrCreateAppYaml(projectPath, stageDirectory);
            progress.Report(0.6);

            var effectiveVersion = options.Version ?? GetDefaultVersion();
            var deployTask = DeployAppBundleAsync(
                stageDirectory: stageDirectory,
                version: effectiveVersion,
                promote: options.Promote,
                context: options.Context,
                outputAction: outputAction);
            if (!await UpdateProgress(deployTask, progress, 0.6, 0.9))
            {
                Debug.WriteLine("Failed to deploy bundle.");
                return null;
            }

            var service = GetAppEngineService(projectPath);
            return new NetCorePublishResult(
                projectId: options.Context.ProjectId,
                service: service,
                version: effectiveVersion,
                promoted: options.Promote);
        }

        private static async Task<bool> UpdateProgress(Task<bool> deployTask, IProgress<double> progress, double from, double to)
        {
            double current = 0.6;
            while (current < to)
            {
                progress.Report(current);

                var resultTask = await Task.WhenAny(deployTask, Task.Delay(10000));
                if (resultTask == deployTask)
                {
                    return await deployTask;
                }

                current += 0.05;
            }
            return await deployTask;
        }

        private static string GetAppEngineService(string projectPath)
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            var appYaml = Path.Combine(projectDirectory, AppYamlName);
            if (!File.Exists(appYaml))
            {
                return "default";
            }
            else
            {
                // TODO: Load the app yaml and look for the service key.
                return "default";
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

            outputAction($"dotnet {arguments}");
            return ProcessUtils.RunCommandAsync(s_dotnetPath.Value, arguments, (o, e) => outputAction(e.Line));
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
    }
}
