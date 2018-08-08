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

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains methods used to maniuplate ASP.NET Core projects.
    /// </summary>
    [Export(typeof(INetCoreAppUtils))]
    public class NetCoreAppUtils : INetCoreAppUtils
    {
        private readonly Lazy<IProcessService> _processService;
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly Lazy<IGCloudWrapper> _gcloudWrapper;
        private readonly Lazy<IEnvironment> _environment;
        private readonly IToolsPathProvider _toolsPathProvider;
        public const string DockerfileName = "Dockerfile";
        private const string RuntimeImageFormat = "gcr.io/google-appengine/aspnetcore:{0}";

        private IProcessService ProcessService => _processService.Value;
        private IFileSystem FileSystem => _fileSystem.Value;
        private IGCloudWrapper GCloudWrapper => _gcloudWrapper.Value;
        public IEnvironment Environment => _environment.Value;

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

        [ImportingConstructor]
        public NetCoreAppUtils(Lazy<IProcessService> processService, Lazy<IFileSystem> fileSystem, Lazy<IGCloudWrapper> gcloudWrapper, Lazy<IEnvironment> environment)
        {
            _processService = processService;
            _fileSystem = fileSystem;
            _gcloudWrapper = gcloudWrapper;
            _environment = environment;
            _toolsPathProvider = VsVersionUtils.ToolsPathProvider;
        }

        /// <summary>
        /// Creates an app bundle by publishing it to the given directory.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="stageDirectory">The directory the build output is published to.</param>
        /// <param name="outputAction">The callback to call with output from the command.</param>
        /// <param name="configuration">The name of the configuration to publish.</param>
        public async Task<bool> CreateAppBundleAsync(
            IParsedProject project,
            string stageDirectory,
            Func<string, OutputStream, Task> outputAction,
            string configuration)
        {
            string arguments = $"publish -o \"{stageDirectory}\" -c {configuration}";
            string externalTools = _toolsPathProvider.GetExternalToolsPath();
            string workingDir = project.DirectoryPath;
            var env = new Dictionary<string, string>
            {
                { "PATH", $"{Environment.GetEnvironmentVariable("PATH")};{externalTools}" }
            };

            Debug.WriteLine($"Using tools from {externalTools}");
            Debug.WriteLine($"Setting working directory to {workingDir}");
            FileSystem.Directory.CreateDirectory(stageDirectory);
            await outputAction($"dotnet {arguments}", OutputStream.StandardOutput);
            bool result = await ProcessService.RunCommandAsync(
                file: _toolsPathProvider.GetDotnetPath(),
                args: arguments,
                handler: outputAction,
                workingDir: workingDir,
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
        public void CopyOrCreateDockerfile(IParsedProject project, string stageDirectory)
        {
            string sourceDockerfile = GetProjectDockerfilePath(project);
            string targetDockerfile = Path.Combine(stageDirectory, DockerfileName);
            string entryPointName = CommonUtils.GetEntrypointName(stageDirectory) ?? project.Name;
            string baseImage = string.Format(RuntimeImageFormat, project.FrameworkVersion);

            if (FileSystem.File.Exists(sourceDockerfile))
            {
                FileSystem.File.Copy(sourceDockerfile, targetDockerfile, overwrite: true);
            }
            else
            {
                string content = string.Format(DockerfileDefaultContent, baseImage, entryPointName);
                FileSystem.File.WriteAllText(targetDockerfile, content);
            }
        }

        /// <summary>
        /// Generates the Dockerfile for this .NET Core project.
        /// </summary>
        /// <param name="project">The project.</param>
        public void GenerateDockerfile(IParsedProject project)
        {
            string targetDockerfile = GetProjectDockerfilePath(project);
            string baseImage = string.Format(RuntimeImageFormat, project.FrameworkVersion);
            string content = string.Format(DockerfileDefaultContent, baseImage, project.Name);
            FileSystem.File.WriteAllText(targetDockerfile, content);
        }

        /// <summary>
        /// Checks if the Dockerfile for the project was created.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>True if the Dockerfile exists, false otherwise.</returns>
        public bool CheckDockerfile(IParsedProject project)
        {
            return FileSystem.File.Exists(GetProjectDockerfilePath(project));
        }

        private static string GetProjectDockerfilePath(IParsedProject project)
        {
            return Path.Combine(project.DirectoryPath, DockerfileName);
        }
    }
}
