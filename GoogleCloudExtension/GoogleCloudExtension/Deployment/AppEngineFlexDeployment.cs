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

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class implements all of the necessary details to deploy an ASP.NET Core application
    /// to the App Engine Flex environment.
    /// </summary>
    [Export(typeof(IAppEngineFlexDeployment))]
    public class AppEngineFlexDeployment : IAppEngineFlexDeployment
    {
        public const string AppYamlName = "app.yaml";
        public const string DockerfileName = NetCoreAppUtils.DockerfileName;

        private const string AppYamlDefaultContent =
            "runtime: aspnetcore\n" +
            "env: flex\n";

        private const string AppYamlDefaultServiceFormat =
            "runtime: aspnetcore\n" +
            "env: flex\n" +
            "service: {0}\n";

        public const string DefaultServiceName = "default";
        private const string ServiceYamlProperty = "service";
        private const string RuntimeYamlProperty = "runtime";

        private const string AspNetCoreRuntime = "aspnetcore";
        private const string CustomRuntime = "custom";

        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly Deserializer _yamlDeserializer = new Deserializer();
        private readonly Serializer _yamlSerializer = new Serializer();

        private IFileSystem FileSystem => _fileSystem.Value;

        /// <summary>
        /// The options for the deployment operation.
        /// </summary>
        public class DeploymentOptions
        {
            public DeploymentOptions(string service, string version, bool promote, bool openWebsite)
            {
                Service = service;
                Version = version;
                Promote = promote;
                OpenWebsite = openWebsite;
                Context = new GCloudContext();
            }

            /// <summary>
            /// The App Engine service to deploy.
            /// </summary>
            public string Service { get; }

            /// <summary>
            /// What version name to use when deploying. If null a default version name based on current time and
            /// date will be used.
            /// </summary>
            public string Version { get; }

            /// <summary>
            /// Whether to promote the new version to receive 100% of the traffic or not.
            /// </summary>
            public bool Promote { get; }

            /// <summary>
            /// The context on which to execute the underlying gcloud command.
            /// </summary>
            public GCloudContext Context { get; }

            /// <summary>
            /// Whether to open the website after deployment.
            /// </summary>
            public bool OpenWebsite { get; }
        }

        [ImportingConstructor]
        public AppEngineFlexDeployment(Lazy<IFileSystem> fileSystem)
        {
            _fileSystem = fileSystem;
        }


        /// <summary>
        /// Publishes the ASP.NET Core project to App Engine Flex and reports progress to the UI.
        /// </summary>
        /// <param name="project">The project to deploy.</param>
        /// <param name="options">The <see cref="AppEngineFlexDeployment.DeploymentOptions"/> to use.</param>
        public async Task PublishProjectAsync(IParsedProject project, DeploymentOptions options)
        {
            try
            {
                ShellUtils.SaveAllFiles();

                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine(string.Format(Resources.FlexPublishStepStartMessage, project.Name));

                TimeSpan deploymentDuration;
                AppEngineFlexDeploymentResult result;
                using (StatusbarHelper.Freeze())
                using (StatusbarHelper.ShowDeployAnimation())
                using (ProgressBarHelper progress =
                    StatusbarHelper.ShowProgressBar(Resources.FlexPublishProgressMessage))
                using (ShellUtils.SetShellUIBusy())
                {
                    DateTime startDeploymentTime = DateTime.Now;
                    result = await PublishProjectAsync(
                        project,
                        options,
                        progress,
                        VsVersionUtils.ToolsPathProvider,
                        GcpOutputWindow.OutputLine);
                    deploymentDuration = DateTime.Now - startDeploymentTime;
                }

                if (result != null)
                {
                    GcpOutputWindow.OutputLine(string.Format(Resources.FlexPublishSuccessMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                    string url = result.GetDeploymentUrl();
                    GcpOutputWindow.OutputLine(string.Format(Resources.PublishUrlMessage, url));
                    if (options.OpenWebsite)
                    {
                        Process.Start(url);
                    }

                    EventsReporterWrapper.ReportEvent(
                        GaeDeployedEvent.Create(CommandStatus.Success, deploymentDuration));
                }
                else
                {
                    GcpOutputWindow.OutputLine(string.Format(Resources.FlexPublishFailedMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Failure, deploymentDuration));
                }
            }
            catch (Exception)
            {
                EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Failure));
                GcpOutputWindow.OutputLine(string.Format(Resources.FlexPublishFailedMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);
            }
        }

        public void SaveServiceToAppYaml(IParsedDteProject publishDialogProject, string service)
        {
            string appYamlPath = GetAppYamlPath(publishDialogProject);
            if (FileSystem.File.Exists(appYamlPath))
            {
                var yamlObject = _yamlDeserializer.Deserialize<Dictionary<object, object>>(File.OpenText(appYamlPath));
                if (service != DefaultServiceName)
                {
                    yamlObject[ServiceYamlProperty] = service;
                }
                else if (yamlObject.ContainsKey(ServiceYamlProperty))
                {
                    yamlObject.Remove(ServiceYamlProperty);
                }
                _yamlSerializer.Serialize(File.CreateText(appYamlPath), yamlObject);
            }
            else
            {
                GenerateAppYaml(publishDialogProject, service);
            }
        }

        /// <summary>
        /// Publishes the ASP.NET Core project to App Engine Flex.
        /// </summary>
        /// <param name="project">The project to deploy.</param>
        /// <param name="options">The <seealso cref="DeploymentOptions"/> to use.</param>
        /// <param name="progress">The progress indicator.</param>
        /// <param name="toolsPathProvider">The tools path provider to use.</param>
        /// <param name="outputAction">The action to call with lines from the command output.</param>
        private async Task<AppEngineFlexDeploymentResult> PublishProjectAsync(
            IParsedProject project,
            DeploymentOptions options,
            IProgress<double> progress,
            IToolsPathProvider toolsPathProvider,
            Action<string> outputAction)
        {
            string stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);
            progress.Report(0.1);

            using (new Disposable(() => CommonUtils.Cleanup(stageDirectory)))
            {
                // Wait for the bundle creation operation to finish, updating progress as it goes.
                Task<bool> createAppBundleTask = NetCoreAppUtils.CreateAppBundleAsync(project, stageDirectory, toolsPathProvider, outputAction);
                if (!await ProgressHelper.UpdateProgress(createAppBundleTask, progress, from: 0.1, to: 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return null;
                }

                var runtime = GetAppEngineRuntime(project);
                CopyOrCreateAppYaml(project, stageDirectory, options);
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
        /// <param name="service"></param>
        public void GenerateAppYaml(IParsedProject project, string service = DefaultServiceName)
        {
            string targetAppYaml = GetAppYamlPath(project);
            if (service == DefaultServiceName)
            {
                FileSystem.File.WriteAllText(targetAppYaml, AppYamlDefaultContent);
            }
            else
            {
                FileSystem.File.WriteAllText(
                    targetAppYaml, string.Format(AppYamlDefaultServiceFormat, service));
            }
        }

        /// <summary>
        /// Checks the project configuration files to see if they exist.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>An instance of <seealso cref="ProjectConfigurationStatus"/> with the status of the config.</returns>
        public ProjectConfigurationStatus CheckProjectConfiguration(IParsedProject project)
        {
            var projectDirectory = project.DirectoryPath;
            var targetAppYaml = Path.Combine(projectDirectory, AppYamlName);
            var hasAppYaml = FileSystem.File.Exists(targetAppYaml);
            var hasDockefile = NetCoreAppUtils.CheckDockerfile(project);

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

        private string GetAppEngineRuntime(IParsedProject project)
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
            string result = defaultValue;
            var propertyName = $"{property}:";

            if (FileSystem.File.Exists(yamlPath))
            {
                try
                {
                    var lines = FileSystem.File.ReadLines(yamlPath);
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

        private string GetDefaultVersion()
        {
            var now = DateTime.Now;
            return String.Format(
                "{0:0000}{1:00}{2:00}t{3:00}{4:00}{5:00}",
                now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second);
        }

        private void CopyOrCreateAppYaml(IParsedProject project, string stageDirectory, DeploymentOptions options)
        {
            string sourceAppYaml = GetAppYamlPath(project);
            string targetAppYaml = Path.Combine(stageDirectory, AppYamlName);

            if (FileSystem.File.Exists(sourceAppYaml))
            {
                if (options.Service != GetAppEngineService(project))
                {
                    StreamReader sourceReader = File.OpenText(sourceAppYaml);
                    var appYamlObject = _yamlDeserializer.Deserialize<Dictionary<object, object>>(sourceReader);
                    appYamlObject[ServiceYamlProperty] = options.Service;
                    _yamlSerializer.Serialize(File.CreateText(targetAppYaml), appYamlObject);
                }
                else
                {
                    FileSystem.File.Copy(sourceAppYaml, targetAppYaml, overwrite: true);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(options.Service) || options.Service == DefaultServiceName)
                {
                    FileSystem.File.WriteAllText(targetAppYaml, AppYamlDefaultContent);
                }
                else
                {
                    FileSystem.File.WriteAllText(targetAppYaml, string.Format(AppYamlDefaultServiceFormat, options.Service));
                }
            }
        }

        private Task<bool> DeployAppBundleAsync(
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
