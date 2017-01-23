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

using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.LinkPrompt;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.GkeStep
{
    /// <summary>
    /// This class represents the deployment wizard's step to deploy an app to GKE.
    /// </summary>
    public class GkeStepViewModel : PublishDialogStepBase
    {
        private readonly GkeStepContent _content;
        private IPublishDialog _publishDialog;
        private Cluster _selectedCluster;
        private string _deploymentName;
        private string _deploymentVersion;
        private bool _exposeService = true;
        private bool _openWebsite = true;

        /// <summary>
        /// The list of clusters that serve as the target for deployment.
        /// </summary>
        public AsyncPropertyValue<IList<Cluster>> Clusters { get; }

        /// <summary>
        /// The currently selected cluster to use for deployment.
        /// </summary>
        public Cluster SelectedCluster
        {
            get { return _selectedCluster; }
            set
            {
                SetValueAndRaise(ref _selectedCluster, value);
                CanPublish = value != null;
            }
        }

        /// <summary>
        /// The name to use for the deployment, by default the name of the project.
        /// </summary>
        public string DeploymentName
        {
            get { return _deploymentName; }
            set { SetValueAndRaise(ref _deploymentName, value); }
        }

        /// <summary>
        /// The version to use for the deployment, by default the date in a similar way to what gcloud uses for 
        /// app engine.
        /// </summary>
        public string DeploymentVersion
        {
            get { return _deploymentVersion; }
            set { SetValueAndRaise(ref _deploymentVersion, value); }
        }

        /// <summary>
        /// Whether a public service should be exposed for this deployment.
        /// </summary>
        public bool ExposeService
        {
            get { return _exposeService; }
            set { SetValueAndRaise(ref _exposeService, value); }
        }

        /// <summary>
        /// Whether the website should be open once a succesful deployment happens.
        /// </summary>
        public bool OpenWebsite
        {
            get { return _openWebsite; }
            set { SetValueAndRaise(ref _openWebsite, value); }
        }

        private GkeStepViewModel(GkeStepContent content)
        {
            _content = content;

            Clusters = new AsyncPropertyValue<IList<Cluster>>(GetAllClustersAsync());
        }

        #region IPublishDialogStep overrides

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;

            DeploymentName = _publishDialog.Project.Name.ToLower();
            DeploymentVersion = GetDefaultVersion();

            // Mark that the dialog is going to be busy until we have loaded the data.
            _publishDialog.TrackTask(Clusters.ValueTask);
        }

        /// <summary>
        /// Start the publish operation.
        /// </summary>
        public override async void Publish()
        {
            var project = _publishDialog.Project;
            try
            {
                var verifyGCloudTask = VerifyGCloudDependencies();
                _publishDialog.TrackTask(verifyGCloudTask);
                if (!await verifyGCloudTask)
                {
                    Debug.WriteLine("Aborting deployment, no kubectl was found.");
                    _publishDialog.FinishFlow();
                    return;
                }

                var context = new GCloudContext
                {
                    CredentialsPath = CredentialsStore.Default.CurrentAccountPath,
                    ProjectId = CredentialsStore.Default.CurrentProjectId,
                    AppName = GoogleCloudExtensionPackage.ApplicationName,
                    AppVersion = GoogleCloudExtensionPackage.ApplicationVersion,
                };
                var options = new GkeDeployment.DeploymentOptions
                {
                    Cluster = SelectedCluster.Name,
                    Zone = SelectedCluster.Zone,
                    DeploymentName = DeploymentName,
                    DeploymentVersion = DeploymentVersion,
                    ExposeService = ExposeService,
                    Context = context,
                    WaitingForServiceIpCallback = () => GcpOutputWindow.OutputLine(Resources.GkePublishWaitingForServiceIpMessage)
                };

                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeployingToGkeMessage, project.Name));

                _publishDialog.FinishFlow();

                GkeDeploymentResult result;
                using (var frozen = StatusbarHelper.Freeze())
                using (var animationShown = StatusbarHelper.ShowDeployAnimation())
                using (var progress = StatusbarHelper.ShowProgressBar(Resources.GkePublishDeploymentStatusMessage))
                using (var deployingOperation = ShellUtils.SetShellUIBusy())
                {
                    result = await GkeDeployment.PublishProjectAsync(
                        project.FullPath,
                        options,
                        progress,
                        GcpOutputWindow.OutputLine);
                }

                if (result != null)
                {
                    GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentSuccessMessage, project.Name));
                    if (result.WasExposed)
                    {
                        if (result.ServiceIpAddress != null)
                        {
                            GcpOutputWindow.OutputLine(
                                String.Format(Resources.GkePublishServiceIpMessage, DeploymentName, result.ServiceIpAddress));
                        }
                        else
                        {
                            GcpOutputWindow.OutputLine(Resources.GkePublishServiceIpTimeoutMessage);
                        }
                    }
                    StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                    if (OpenWebsite && result.WasExposed && result.ServiceIpAddress != null)
                    {
                        Process.Start($"http://{result.ServiceIpAddress}");
                    }
                }
                else
                {
                    GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);
            }
        }

        #endregion

        private async Task<IList<Cluster>> GetAllClustersAsync()
        {
            var dataSource = new GkeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var clusters = await dataSource.GetClusterListAsync();
            return clusters.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Creates a GKE step complete with behavior and visuals.
        /// </summary>
        internal static GkeStepViewModel CreateStep()
        {
            var content = new GkeStepContent();
            var viewModel = new GkeStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }

        private static string GetDefaultVersion()
        {
            var now = DateTime.Now;
            return String.Format(
                "{0:0000}{1:00}{2:00}t{3:00}{4:00}{5:00}",
                now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second);
        }

        private async Task<bool> VerifyGCloudDependencies()
        {
            if (!await GCloudWrapper.CanUseGKEAsync())
            {
                if (!GCloudWrapper.IsGCloudCliInstalled())
                {
                    LinkPromptDialogWindow.PromptUser(
                        Resources.ResetPasswordMissingGcloudTitle,
                        Resources.ResetPasswordGcloudMissingMessage,
                        new LinkInfo(link: "https://cloud.google.com/sdk/", caption: Resources.ResetPasswordGcloudLinkCaption));
                }
                else
                {
                    UserPromptUtils.ErrorPrompt(
                        message: Resources.GkePublishMissingKubectlMessage,
                        title: Resources.GcloudMissingComponentTitle);
                }
                return false;
            }

            return true;
        }
    }
}
