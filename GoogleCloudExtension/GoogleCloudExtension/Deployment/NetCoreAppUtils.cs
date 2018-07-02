﻿// Copyright 2017 Google Inc. All Rights Reserved.
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
    /// This class contains methods used to maniuplate ASP.NET Core projects.
    /// </summary>
    public static class NetCoreAppUtils
    {
        public const string DockerfileName = "Dockerfile";
        private const string RuntimeImageFormat = "gcr.io/google-appengine/aspnetcore:{0}";

        /// <summary>
        /// This template is the smallest possible Dockerfile needed to deploy an ASP.NET Core app to
        /// App Engine Flex environment. It invokes the entry point .dll given by {1}.
        /// The parameters into the string are:
        ///   {0}, the base image for the app's Docker image.
        ///   {1}, the entrypoint .dll for the app.
        /// </summary>
        private const string DockerfileDefaultContent =
            "FROM {0}\n" +
            "COPY . /app\n" +
            "WORKDIR /app\n" +
            "ENTRYPOINT [\"dotnet\", \"{1}.dll\"]\n";

        /// <summary>
        /// Creates an app bundle by publishing it to the given directory.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="stageDirectory">The directory to which to publish.</param>
        /// <param name="pathsProvider">The provider for paths.</param>
        /// <param name="outputAction">The callback to call with output from the command.</param>
        /// <param name="configuration">The name of the configuration to publish.</param>
        public static async Task<bool> CreateAppBundleAsync(
            IParsedProject project,
            string stageDirectory,
            IToolsPathProvider pathsProvider,
            Action<string> outputAction,
            string configuration)
        {
            var arguments = $"publish -o \"{stageDirectory}\" -c {configuration}";
            var externalTools = pathsProvider.GetExternalToolsPath();
            var workingDir = project.DirectoryPath;
            var env = new Dictionary<string, string>
            {
                { "PATH", $"{Environment.GetEnvironmentVariable("PATH")};{externalTools}" }
            };

            Debug.WriteLine($"Using tools from {externalTools}");
            Debug.WriteLine($"Setting working directory to {workingDir}");
            Directory.CreateDirectory(stageDirectory);
            outputAction($"dotnet {arguments}");
            bool result = await ProcessUtils.Default.RunCommandAsync(
                file: pathsProvider.GetDotnetPath(),
                args: arguments,
                workingDir: workingDir,
                handler: (o, e) => outputAction(e.Line),
                environment: env);
            await GCloudWrapper.GenerateSourceContextAsync(project.DirectoryPath, stageDirectory);
            return result;
        }

        /// <summary>
        /// Creates the Dockerfile necessary to package up an ASP.NET Core app if one is not already present at the root
        /// path of the project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="stageDirectory">The directory where to save the Dockerfile.</param>
        public static void CopyOrCreateDockerfile(IParsedProject project, string stageDirectory)
        {
            string sourceDockerfile = Path.Combine(project.DirectoryPath, DockerfileName);
            string targetDockerfile = Path.Combine(stageDirectory, DockerfileName);
            string entryPointName = CommonUtils.GetEntrypointName(stageDirectory) ?? project.Name;
            string baseImage = string.Format(RuntimeImageFormat, project.FrameworkVersion);

            if (File.Exists(sourceDockerfile))
            {
                File.Copy(sourceDockerfile, targetDockerfile, overwrite: true);
            }
            else
            {
                string content = string.Format(DockerfileDefaultContent, baseImage, entryPointName);
                File.WriteAllText(targetDockerfile, content);
            }
        }

        /// <summary>
        /// Generates the Dockerfile for this .NET Core project.
        /// </summary>
        /// <param name="project">The project.</param>
        public static void GenerateDockerfile(IParsedProject project)
        {
            string targetDockerfile = Path.Combine(project.DirectoryPath, DockerfileName);
            string baseImage = string.Format(RuntimeImageFormat, project.FrameworkVersion);
            string content = string.Format(DockerfileDefaultContent, baseImage, project.Name);
            File.WriteAllText(targetDockerfile, content);
        }

        /// <summary>
        /// Checks if the Dockerfile for the project was created.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>True if the Dockerfile exists, false otherwise.</returns>
        public static bool CheckDockerfile(IParsedProject project)
        {
            string targetDockerfile = Path.Combine(project.DirectoryPath, DockerfileName);
            return File.Exists(targetDockerfile);
        }
    }
}
