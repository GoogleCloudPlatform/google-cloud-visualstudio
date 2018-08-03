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
        private readonly Lazy<IStatusbarService> _statusbarService;
        private readonly Lazy<INetCoreAppUtils> _netCoreAppUtils;

        private IShellUtils ShellUtils => _shellUtils.Value;
        private IGcpOutputWindow GcpOutputWindow => _gcpOutputWindow.Value;
        private IAppEngineConfiguration ConfigurationService => _configurationService.Value;
        private IStatusbarService StatusbarHelper => _statusbarService.Value;
        private INetCoreAppUtils NetCoreAppUtils => _netCoreAppUtils.Value;

        /// <summary>
        /// The options for the deployment operation.
        /// </summary>
        public class DeploymentOptions
        {
            public DeploymentOptions(string service, string version, bool promote, bool openWebsite, string configuration)
            {
                Service = service;
                Version = version ?? GcpPublishStepsUtils.GetDefaultVersion();
                Promote = promote;
                OpenWebsite = openWebsite;
                Configuration = configuration;
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

            /// <summary>
            /// The name of the Configuration to publish.
            /// </summary>
            public string Configuration { get; }
        }

        [ImportingConstructor]
        public AppEngineFlexDeployment(
            Lazy<IShellUtils> shellUtils,
            Lazy<IGcpOutputWindow> gcpOutputWindow,
            Lazy<IAppEngineConfiguration> configurationService,
            Lazy<IStatusbarService> statusbarService,
            Lazy<INetCoreAppUtils> netCoreAppUtils)
        {
            _shellUtils = shellUtils;
            _gcpOutputWindow = gcpOutputWindow;
            _configurationService = configurationService;
            _statusbarService = statusbarService;
            _netCoreAppUtils = netCoreAppUtils;
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
                await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
                ShellUtils.SaveAllFiles();

                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine(string.Format(Resources.FlexPublishStepStartMessage, project.Name));

                TimeSpan deploymentDuration;
                AppEngineFlexDeploymentResult result;
                using (StatusbarHelper.Freeze())
                using (StatusbarHelper.ShowDeployAnimation())
                using (IDisposableProgress progress =
                    StatusbarHelper.ShowProgressBar(Resources.FlexPublishProgressMessage))
                using (ShellUtils.SetShellUIBusy())
                {
                    DateTime startDeploymentTime = DateTime.Now;
                    result = await PublishProjectAsync(project, options, progress);
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
        private async Task<AppEngineFlexDeploymentResult> PublishProjectAsync(
            IParsedProject project,
            DeploymentOptions options,
            IProgress<double> progress)
        {
            string stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);
            await progress.ReportAsync(0.1);

            using (new Disposable(() => CommonUtils.Cleanup(stageDirectory)))
            {
                // Wait for the bundle creation operation to finish, updating progress as it goes.
                Task<bool> createAppBundleTask = NetCoreAppUtils.CreateAppBundleAsync(
                    project,
                    stageDirectory,
                    GcpOutputWindow.OutputLine,
                    options.Configuration);
                if (!await progress.UpdateProgressAsync(createAppBundleTask, 0.1, 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return null;
                }

                string runtime = ConfigurationService.GetAppEngineRuntime(project);
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
                await progress.ReportAsync(0.4);

                // Deploy to app engine, this is where most of the time is going to be spent. Wait for
                // the operation to finish, update the progress as it goes.
                Task<bool> deployTask = DeployAppBundleAsync(
                    stageDirectory: stageDirectory,
                    version: options.Version,
                    promote: options.Promote,
                    context: options.Context,
                    outputAction: GcpOutputWindow.OutputLine);
                if (!await progress.UpdateProgressAsync(deployTask, 0.6, 0.9))
                {
                    Debug.WriteLine("Failed to deploy bundle.");
                    return null;
                }
                await progress.ReportAsync(1.0);

                string service = options.Service ?? ConfigurationService.GetAppEngineService(project);
                return new AppEngineFlexDeploymentResult(
                    projectId: options.Context.ProjectId,
                    service: service,
                    version: options.Version,
                    promoted: options.Promote);
            }
        }

        private Task<bool> DeployAppBundleAsync(
            string stageDirectory,
            string version,
            bool promote,
            IGCloudContext context,
            Action<string> outputAction)
        {
            string appYamlPath = Path.Combine(stageDirectory, AppEngineConfiguration.AppYamlName);
            return context.DeployAppAsync(appYamlPath, version, promote, outputAction);
        }
    }
}
