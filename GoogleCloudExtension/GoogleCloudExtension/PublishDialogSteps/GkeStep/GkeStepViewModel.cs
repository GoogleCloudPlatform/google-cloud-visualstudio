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

using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.LinkPrompt;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogleCloudExtension.PublishDialogSteps.GkeStep
{
    /// <summary>
    /// This class represents the deployment wizard's step to deploy an app to GKE.
    /// </summary>
    public class GkeStepViewModel : PublishDialogStepBase
    {
        private static readonly Cluster s_placeholderCluster = new Cluster { Name = Resources.GkePublishNoClustersPlaceholder };
        private static readonly IList<Cluster> s_placeholderList = new List<Cluster> { s_placeholderCluster };

        private readonly GkeStepContent _content;
        private IPublishDialog _publishDialog;
        private AsyncPropertyValue<IList<Cluster>> _clusters;
        private Cluster _selectedCluster;
        private string _deploymentName;
        private string _deploymentVersion;
        private bool _exposeService = true;
        private bool _openWebsite = true;
        private string _replicas = "3";

        /// <summary>
        /// The list of clusters that serve as the target for deployment.
        /// </summary>
        public AsyncPropertyValue<IList<Cluster>> Clusters
        {
            get { return _clusters; }
            private set { SetValueAndRaise(ref _clusters, value); }
        }

        /// <summary>
        /// The currently selected cluster to use for deployment.
        /// </summary>
        public Cluster SelectedCluster
        {
            get { return _selectedCluster; }
            set
            {
                SetValueAndRaise(ref _selectedCluster, value);
                CanPublish = value != null && value != s_placeholderCluster;
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
        /// The number of replicas to create.
        /// </summary>
        public string Replicas
        {
            get { return _replicas; }
            set { SetValueAndRaise(ref _replicas, value); }
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

        /// <summary>
        /// Command to execute to create a new cluster.
        /// </summary>
        public ICommand CreateClusterCommand { get; }

        /// <summary>
        /// Command to execute to refresh the list of clusters.
        /// </summary>
        public ICommand RefreshClustersListCommand { get; }

        private GkeStepViewModel(GkeStepContent content)
        {
            _content = content;

            Clusters = new AsyncPropertyValue<IList<Cluster>>(GetAllClustersAsync());
            CreateClusterCommand = new ProtectedCommand(OnCreateClusterCommand);
            RefreshClustersListCommand = new ProtectedCommand(OnRefreshClustersListCommand);
        }

        private void OnRefreshClustersListCommand()
        {
            var refreshTask = GetAllClustersAsync();
            Clusters = new AsyncPropertyValue<IList<Cluster>>(refreshTask);
            _publishDialog.TrackTask(refreshTask);
        }

        private void OnCreateClusterCommand()
        {
            Process.Start($"https://console.cloud.google.com/kubernetes/add?project={CredentialsStore.Default.CurrentProjectId}");
        }

        #region IPublishDialogStep overrides

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;

            DeploymentName = _publishDialog.Project.Name.ToLower();
            DeploymentVersion = GcpPublishStepsUtils.GetDefaultVersion();

            // Mark that the dialog is going to be busy until we have loaded the data.
            _publishDialog.TrackTask(Clusters.ValueTask);
        }

        /// <summary>
        /// Start the publish operation.
        /// </summary>
        public override async void Publish()
        {
            if (!ValidateInput())
            {
                Debug.WriteLine("Invalid input cancelled the operation.");
                return;
            }

            var project = _publishDialog.Project;
            try
            {
                ShellUtils.SaveAllFiles();

                var verifyGCloudTask = VerifyGCloudDependencies();
                _publishDialog.TrackTask(verifyGCloudTask);
                if (!await verifyGCloudTask)
                {
                    Debug.WriteLine("Aborting deployment, no kubectl was found.");
                    return;
                }

                var gcloudContext = new GCloudContext
                {
                    CredentialsPath = CredentialsStore.Default.CurrentAccountPath,
                    ProjectId = CredentialsStore.Default.CurrentProjectId,
                    AppName = GoogleCloudExtensionPackage.ApplicationName,
                    AppVersion = GoogleCloudExtensionPackage.ApplicationVersion,
                };

                var kubectlContextTask = GCloudWrapper.GetKubectlContextForClusterAsync(
                    cluster: SelectedCluster.Name,
                    zone: SelectedCluster.Zone,
                    context: gcloudContext);
                _publishDialog.TrackTask(kubectlContextTask);

                using (var kubectlContext = await kubectlContextTask)
                {
                    var deploymentExistsTask = KubectlWrapper.DeploymentExistsAsync(DeploymentName, kubectlContext);
                    _publishDialog.TrackTask(deploymentExistsTask);
                    if (await deploymentExistsTask)
                    {
                        if (!UserPromptUtils.ActionPrompt(
                                String.Format(Resources.GkePublishDeploymentAlreadyExistsMessage, DeploymentName),
                                Resources.GkePublishDeploymentAlreadyExistsTitle,
                                actionCaption: Resources.UiUpdateButtonCaption))
                        {
                            return;
                        }
                    }

                    var options = new GkeDeployment.DeploymentOptions
                    {
                        Cluster = SelectedCluster.Name,
                        Zone = SelectedCluster.Zone,
                        DeploymentName = DeploymentName,
                        DeploymentVersion = DeploymentVersion,
                        ExposeService = ExposeService,
                        GCloudContext = gcloudContext,
                        KubectlContext = kubectlContext,
                        Replicas = int.Parse(Replicas),
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
                        if (result.DeploymentUpdated)
                        {
                            GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentUpdatedMessage, options.DeploymentName));
                        }
                        if (result.DeploymentScaled)
                        {
                            GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentScaledMessage, options.DeploymentName, options.Replicas));
                        }

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
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);
                _publishDialog.FinishFlow();
            }
        }

        #endregion

        private bool ValidateInput()
        {
            int replicas = 0;
            if (!int.TryParse(Replicas, out replicas))
            {
                UserPromptUtils.ErrorPrompt(Resources.GkePublishInvalidReplicasMessage, Resources.UiInvalidValueTitle);
                return false;
            }

            if (String.IsNullOrEmpty(DeploymentName))
            {
                UserPromptUtils.ErrorPrompt(Resources.GkePublishEmptyDeploymentNameMessage, Resources.UiInvalidValueTitle);
                return false;
            }
            if (String.IsNullOrEmpty(DeploymentVersion))
            {
                UserPromptUtils.ErrorPrompt(Resources.GkePublishEmptyDeploymentVersionMessage, Resources.UiInvalidValueTitle);
                return false;
            }

            if (!GcpPublishStepsUtils.IsValidName(DeploymentName))
            {
                UserPromptUtils.ErrorPrompt(
                    String.Format(Resources.GkePublishInvalidDeploymentNameMessage, DeploymentName),
                    Resources.UiInvalidValueTitle);
                return false;
            }
            if (!GcpPublishStepsUtils.IsValidName(DeploymentVersion))
            {
                UserPromptUtils.ErrorPrompt(
                    String.Format(Resources.GkePublishInvalidDeploymentVersionMessage, DeploymentVersion),
                    Resources.UiInvalidValueTitle);
                return false;
            }

            return true;
        }

        private async Task<IList<Cluster>> GetAllClustersAsync()
        {
            var dataSource = new GkeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var clusters = await dataSource.GetClusterListAsync();

            var result = clusters?.OrderBy(x => x.Name)?.ToList();
            if (result == null || result.Count == 0)
            {
                return s_placeholderList;
            }
            return result;
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
