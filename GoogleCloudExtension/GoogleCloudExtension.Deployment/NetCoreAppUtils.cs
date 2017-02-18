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

using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains methods used to maniuplate ASP.NET Core projects.
    /// </summary>
    internal static class NetCoreAppUtils
    {
        internal const string DockerfileName = "Dockerfile";

        private static readonly Lazy<string> s_dotnetPath = new Lazy<string>(GetDotnetPath);

        /// <summary>
        /// This template is the smallest possible Dockerfile needed to deploy an ASP.NET Core app to
        /// App Engine Flex environment. It invokes the entry point .dll given by {0}, sets up the environment
        /// so the app listens on port 8080.
        /// All of the files composing the app are copied to the /app path, then it is set as the working directory.
        /// </summary>
        private const string DockerfileDefaultContent =
            "FROM gcr.io/google-appengine/aspnetcore:1.0.3\n" +
            "COPY . /app\n" +
            "WORKDIR /app\n" +
            "EXPOSE 8080\n" +
            "ENV ASPNETCORE_URLS=http://*:8080\n" +
            "ENTRYPOINT [\"dotnet\", \"{0}.dll\"]\n";

        /// <summary>
        /// Creates an app bundle by publishing it to the given directory. It only publishes the release configuration.
        /// </summary>
        /// <param name="projectPath">The full path to the project to publish.</param>
        /// <param name="stageDirectory">The directory to which to publish.</param>
        /// <param name="outputAction">The callback to call with output from the command.</param>
        internal static Task<bool> CreateAppBundleAsync(string projectPath, string stageDirectory, Action<string> outputAction)
        {
            var arguments = $"publish -o \"{stageDirectory}\" -c Release";
            var externalTools = GetExternalToolsPath();
            var workingDir = Path.GetDirectoryName(projectPath);
            var env = new Dictionary<string, string>
            {
                { "PATH", $"{Environment.GetEnvironmentVariable("PATH")};{externalTools}" },
            };

            Debug.WriteLine($"Using tools from {externalTools}");
            Debug.WriteLine($"Setting working directory to {workingDir}");
            outputAction($"dotnet {arguments}");
            return ProcessUtils.RunCommandAsync(
                file: s_dotnetPath.Value,
                args: arguments,
                workingDir: workingDir,
                handler: (o, e) => outputAction(e.Line),
                environment: env);
        }

        /// <summary>
        /// Creates the Dockerfile necessary to package up an ASP.NET Core app if one is not already present at the root
        /// path of the project.
        /// </summary>
        /// <param name="projectPath">The full path to the project.json.</param>
        /// <param name="stageDirectory">The directory where to save the Dockerfile.</param>
        internal static void CopyOrCreateDockerfile(string projectPath, string stageDirectory)
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
                var content = String.Format(DockerfileDefaultContent, CommonUtils.GetProjectName(projectPath));
                File.WriteAllText(targetDockerfile, content);
            }
        }

        /// <summary>
        /// Generates the Dockerfile for this .NET Core project.
        /// </summary>
        /// <param name="projectPath">The full path to the project.json file for the project.</param>
        internal static void GenerateDockerfile(string projectPath)
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            var targetDockerfile = Path.Combine(projectDirectory, DockerfileName);
            var content = String.Format(DockerfileDefaultContent, CommonUtils.GetProjectName(projectPath));
            File.WriteAllText(targetDockerfile, content);
        }

        /// <summary>
        /// Checks if the Dockerfile for the project was created.
        /// </summary>
        /// <param name="projectPath">The full path to the project.json for the project.</param>
        /// <returns>True if the Dockerfile exists, false otherwise.</returns>
        internal static bool CheckDockerfile(string projectPath)
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            var targetDockerfile = Path.Combine(projectDirectory, DockerfileName);
            return File.Exists(targetDockerfile);
        }

        private static string GetExternalToolsPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            return Path.Combine(programFilesPath, @"Microsoft Visual Studio 14.0\Web\External");
        }

        private static string GetDotnetPath()
        {
            var programFilesPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            return Path.Combine(programFilesPath, @"dotnet\dotnet.exe");
        }
    }
}
