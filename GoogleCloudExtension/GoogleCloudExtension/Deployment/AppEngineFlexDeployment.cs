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
using GoogleCloudExtension.Services.Configuration;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class implements all of the necessary details to deploy an ASP.NET Core application
    /// to the App Engine Flex environment.
    /// </summary>
    [Export(typeof(IAppEngineFlexDeployment))]
    public class AppEngineFlexDeployment : IAppEngineFlexDeployment
    {
        private readonly Lazy<IShellUtils> _shellUtils;
        private readonly Lazy<IGcpOutputWindow> _gcpOutputWindow;
        private readonly Lazy<IAppEngineConfiguration> _configurationService;

        private IShellUtils ShellUtils => _shellUtils.Value;
        private IGcpOutputWindow GcpOutputWindow => _gcpOutputWindow.Value;
        private IAppEngineConfiguration ConfigurationService => _configurationService.Value;

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
        public AppEngineFlexDeployment(Lazy<IShellUtils> shellUtils, Lazy<IGcpOutputWindow> gcpOutputWindow, Lazy<IAppEngineConfiguration> configurationService)
        {
            _shellUtils = shellUtils;
            _gcpOutputWindow = gcpOutputWindow;
            _configurationService = configurationService;
        }


        /// <summary>
        /// Publishes the ASP.NET Core project to App Engine Flex and reports progress to the UI.
        /// </summary>
        /// <param name="project">The project to deploy.</param>
        /// <param name="options">The <see cref="DeploymentOptions"/> to use.</param>
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

                var runtime = ConfigurationService.GetAppEngineRuntime(project);
                ConfigurationService.CopyOrCreateAppYaml(project, stageDirectory, options.Service);
                if (runtime == AppEngineConfiguration.CustomRuntime)
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
                var effectiveVersion = options.Version ?? GcpPublishStepsUtils.GetDefaultVersion();
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

                var service = options.Service ?? ConfigurationService.GetAppEngineService(project);
                return new AppEngineFlexDeploymentResult(
                    projectId: options.Context.ProjectId,
                    service: service,
                    version: effectiveVersion,
                    promoted: options.Promote);
            }
        }

        private Task<bool> DeployAppBundleAsync(
            string stageDirectory,
            string version,
            bool promote,
            GCloudContext context,
            Action<string> outputAction)
        {
            string appYamlPath = Path.Combine(stageDirectory, AppEngineConfiguration.AppYamlName);
            return GCloudWrapper.DeployAppAsync(
                appYaml: appYamlPath,
                version: version,
                promote: promote,
                outputAction: outputAction,
                context: context);
        }
    }
}
