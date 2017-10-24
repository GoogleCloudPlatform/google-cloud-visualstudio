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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using GoogleCloudExtension.VsVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        // The APIs required for a succesful deployment to GKE.
        private static readonly IEnumerable<string> s_requiredApis = new List<string>
        {
            // Need the GKE API to be able to list clusters.
            KnownApis.ContainerEngineApiName,

            // Need the Cloud Builder API to actually deploy.
            KnownApis.CloudBuildApiName
        };

        private readonly GkeStepContent _content;
        private AsyncProperty<IEnumerable<Cluster>> _clusters;
        private Cluster _selectedCluster;
        private string _deploymentName;
        private string _deploymentVersion;
        private bool _dontExposeService = true;
        private bool _exposeService = false;
        private bool _exposePublicService = false;
        private bool _openWebsite = false;
        private string _replicas = "3";

        /// <summary>
        /// The list of clusters that serve as the target for deployment.
        /// </summary>
        public AsyncProperty<IEnumerable<Cluster>> Clusters
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
                UpdateCanPublish();
            }
        }

        /// <summary>
        /// The name to use for the deployment, by default the name of the project.
        /// </summary>
        public string DeploymentName
        {
            get { return _deploymentName; }
            set
            {
                IEnumerable<ValidationResult> validations =
                    GcpPublishStepsUtils.ValidateName(value, Resources.GkePublishDeploymentNameFieldName);
                SetAndRaiseWithValidation(ref _deploymentName, value, validations);
            }
        }

        /// <summary>
        /// The version to use for the deployment, by default the date in a similar way to what gcloud uses for
        /// app engine.
        /// </summary>
        public string DeploymentVersion
        {
            get { return _deploymentVersion; }
            set
            {
                IEnumerable<ValidationResult> validations =
                    GcpPublishStepsUtils.ValidateName(value, Resources.GkePublishDeploymentVersionFieldName);
                SetAndRaiseWithValidation(ref _deploymentVersion, value, validations);
            }
        }

        /// <summary>
        /// The number of replicas to create.
        /// </summary>
        public string Replicas
        {
            get { return _replicas; }
            set
            {
                IEnumerable<ValidationResult> validations =
                    GcpPublishStepsUtils.ValidateInteger(value, Resources.GkePublishReplicasFieldName);
                SetAndRaiseWithValidation(ref _replicas, value, validations);
            }
        }

        /// <summary>
        /// Whether the service should NOT be exposed, the opposite of <seealso cref="ExposeService"/>.
        /// </summary>
        public bool DontExposeService
        {
            get { return _dontExposeService; }
            set { SetValueAndRaise(ref _dontExposeService, value); }
        }

        /// <summary>
        /// Whether a service should be exposed for this deployment.
        /// </summary>
        public bool ExposeService
        {
            get { return _exposeService; }
            set
            {
                SetValueAndRaise(ref _exposeService, value);
                InvalidateExposeService();
            }
        }

        /// <summary>
        /// Whether the service to be exposed should be public on the internet or not.
        /// </summary>
        public bool ExposePublicService
        {
            get { return _exposePublicService; }
            set { SetValueAndRaise(ref _exposePublicService, value); }
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

            Clusters = new AsyncProperty<IEnumerable<Cluster>>(GetAllClustersAsync());
            CreateClusterCommand = new ProtectedCommand(OnCreateClusterCommand);
            RefreshClustersListCommand = new ProtectedCommand(RefreshClustersList);

            CredentialsStore.Default.CurrentProjectIdChanged += (sender, args) =>
            {
                RefreshClustersList();
            };
        }

        private void UpdateCanPublish()
        {
            CanPublish = SelectedCluster != null
                && SelectedCluster != s_placeholderCluster
                && !HasErrors;
        }

        protected override void HasErrorsChanged()
        {
            UpdateCanPublish();
        }

        private void RefreshClustersList()
        {
            Task<IEnumerable<Cluster>> refreshTask = GetAllClustersAsync();
            Clusters = new AsyncProperty<IEnumerable<Cluster>>(refreshTask);
            PublishDialog.TrackTask(refreshTask);
        }

        private void OnCreateClusterCommand()
        {
            Process.Start($"https://console.cloud.google.com/kubernetes/add?project={CredentialsStore.Default.CurrentProjectId}");
        }

        #region IPublishDialogStep overrides

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            base.OnPushedToDialog(dialog);

            DeploymentName = PublishDialog.Project.Name.ToLower();
            DeploymentVersion = GcpPublishStepsUtils.GetDefaultVersion();

            // Mark that the dialog is going to be busy until we have loaded the data.
            PublishDialog.TrackTask(Clusters.ValueTask);
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

            var project = PublishDialog.Project;
            try
            {
                ShellUtils.SaveAllFiles();

                var verifyGCloudTask = GCloudWrapperUtils.VerifyGCloudDependencies(GCloudComponent.Kubectl);
                PublishDialog.TrackTask(verifyGCloudTask);
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
                PublishDialog.TrackTask(kubectlContextTask);

                using (var kubectlContext = await kubectlContextTask)
                {
                    var deploymentExistsTask = KubectlWrapper.DeploymentExistsAsync(DeploymentName, kubectlContext);
                    PublishDialog.TrackTask(deploymentExistsTask);
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
                        ExposePublicService = ExposePublicService,
                        GCloudContext = gcloudContext,
                        KubectlContext = kubectlContext,
                        Replicas = int.Parse(Replicas),
                        WaitingForServiceIpCallback = () => GcpOutputWindow.OutputLine(Resources.GkePublishWaitingForServiceIpMessage)
                    };

                    GcpOutputWindow.Activate();
                    GcpOutputWindow.Clear();
                    GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeployingToGkeMessage, project.Name));

                    PublishDialog.FinishFlow();

                    TimeSpan deploymentDuration;
                    GkeDeploymentResult result;
                    using (StatusbarHelper.Freeze())
                    using (StatusbarHelper.ShowDeployAnimation())
                    using (var progress = StatusbarHelper.ShowProgressBar(Resources.GkePublishDeploymentStatusMessage))
                    using (ShellUtils.SetShellUIBusy())
                    {
                        var deploymentStartTime = DateTime.Now;
                        result = await GkeDeployment.PublishProjectAsync(
                            project,
                            options,
                            progress,
                            VsVersionUtils.ToolsPathProvider,
                            GcpOutputWindow.OutputLine);
                        deploymentDuration = DateTime.Now - deploymentStartTime;
                    }

                    if (result != null)
                    {
                        OutputResultData(result, options);

                        StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                        if (OpenWebsite && result.ServiceExposed && result.PublicServiceIpAddress != null)
                        {
                            Process.Start($"http://{result.PublicServiceIpAddress}");
                        }

                        EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Success, deploymentDuration));
                    }
                    else
                    {
                        GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                        StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                        EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Failure));
                    }
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);
                PublishDialog.FinishFlow();

                EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        private void OutputResultData(GkeDeploymentResult result, GkeDeployment.DeploymentOptions options)
        {
            GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentSuccessMessage, PublishDialog.Project.Name));
            if (result.DeploymentUpdated)
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishDeploymentUpdatedMessage, options.DeploymentName));
            }
            if (result.DeploymentScaled)
            {
                GcpOutputWindow.OutputLine(
                    String.Format(Resources.GkePublishDeploymentScaledMessage, options.DeploymentName, options.Replicas));
            }

            if (result.ServiceUpdated)
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishServiceUpdatedMessage, options.DeploymentName));
            }
            if (result.ServiceExposed)
            {
                if (result.PublicServiceIpAddress != null)
                {
                    GcpOutputWindow.OutputLine(
                        String.Format(Resources.GkePublishServiceIpMessage, DeploymentName, result.PublicServiceIpAddress));
                }
                else
                {
                    if (ExposePublicService)
                    {
                        GcpOutputWindow.OutputLine(Resources.GkePublishServiceIpTimeoutMessage);
                    }
                    else
                    {
                        GcpOutputWindow.OutputLine(
                            String.Format(
                                Resources.GkePublishServiceClusterIpMessage, DeploymentName, result.ClusterServiceIpAddress));
                    }
                }
            }
            if (result.ServiceDeleted)
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.GkePublishServiceDeletedMessage, DeploymentName));
            }
        }

        #endregion

        private void InvalidateExposeService()
        {
            if (!ExposeService)
            {
                ExposePublicService = false;
                OpenWebsite = false;
            }
        }

        private bool ValidateInput()
        {
            int replicas;
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

            if (!GcpPublishStepsUtils.IsValidName(DeploymentName))
            {
                UserPromptUtils.ErrorPrompt(
                    String.Format(Resources.GkePublishInvalidDeploymentNameMessage, DeploymentName),
                    Resources.UiInvalidValueTitle);
                return false;
            }
            if (String.IsNullOrEmpty(DeploymentVersion))
            {
                UserPromptUtils.ErrorPrompt(Resources.GkePublishEmptyDeploymentVersionMessage, Resources.UiInvalidValueTitle);
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

        private async Task<IEnumerable<Cluster>> GetAllClustersAsync()
        {
            if (!await ApiManager.Default.EnsureAllServicesEnabledAsync(
                    s_requiredApis,
                    Resources.GkePublishEnableApiMessage))
            {
                PublishDialog.FinishFlow();
                return Enumerable.Empty<Cluster>();
            }

            var dataSource = new GkeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var clusters = await dataSource.GetClusterListAsync();

            var result = clusters?.OrderBy(x => x.Name).ToList();
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
    }
}
