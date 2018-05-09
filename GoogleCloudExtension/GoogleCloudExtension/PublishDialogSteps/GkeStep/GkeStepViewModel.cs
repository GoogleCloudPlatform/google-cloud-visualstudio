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

using Google.Apis.CloudResourceManager.v1.Data;
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
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialogSteps.GkeStep
{
    /// <summary>
    /// This class represents the deployment wizard's step to deploy an app to GKE.
    /// </summary>
    public class GkeStepViewModel : PublishDialogStepBase
    {
        internal static readonly Cluster s_placeholderCluster = new Cluster { Name = Resources.GkePublishNoClustersPlaceholder };
        internal static readonly IList<Cluster> s_placeholderList = new List<Cluster> { s_placeholderCluster };
        internal const string ReplicasDefaultValue = "3";
        internal const string GkeAddClusterUrlFormat = "https://console.cloud.google.com/kubernetes/add?project={0}";

        // The APIs required for a succesful deployment to GKE.
        private static readonly IList<string> s_requiredApis = new List<string>
        {
            // Need the GKE API to be able to list clusters.
            KnownApis.ContainerEngineApiName,

            // Need the Cloud Builder API to actually deploy.
            KnownApis.CloudBuildApiName
        };

        private readonly GkeStepContent _content;
        private readonly IGkeDataSource _dataSource;
        private IEnumerable<Cluster> _clusters = Enumerable.Empty<Cluster>();
        private Cluster _selectedCluster = null;
        private string _deploymentName = null;
        private string _deploymentVersion = GcpPublishStepsUtils.GetDefaultVersion();
        private bool _dontExposeService = true;
        private bool _exposeService = false;
        private bool _exposePublicService = false;
        private bool _openWebsite = false;
        private string _replicas = ReplicasDefaultValue;
        private Func<string, Process> StartProcess => _startProcessOverride ?? Process.Start;
        internal Func<string, Process> _startProcessOverride;

        /// <summary>
        /// The list of clusters that serve as the target for deployment.
        /// </summary>
        public IEnumerable<Cluster> Clusters
        {
            get { return _clusters; }
            private set
            {
                SetValueAndRaise(ref _clusters, value);
                SelectedCluster = value?.FirstOrDefault();
            }
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
                RefreshCanPublish();
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
                    GcpPublishStepsUtils.ValidatePositiveNonZeroInteger(value, Resources.GkePublishReplicasFieldName);
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
        public ProtectedCommand CreateClusterCommand { get; }

        /// <summary>
        /// Command to execute to refresh the list of clusters.
        /// </summary>
        public ProtectedCommand RefreshClustersListCommand { get; }

        private IGkeDataSource CurrentDataSource => _dataSource ?? new GkeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);

        private GkeStepViewModel(GkeStepContent content, IGkeDataSource dataSource, IApiManager apiManager, Func<Project> pickProjectPrompt)
            : base(apiManager, pickProjectPrompt)
        {
            _content = content;
            _dataSource = dataSource;

            CreateClusterCommand = new ProtectedCommand(OnCreateClusterCommand, canExecuteCommand: false);
            RefreshClustersListCommand = new ProtectedAsyncCommand(async () =>
            {
                StartAndTrack(OnRefreshClustersListCommand);
                await AsyncAction;
            }, false);
        }

        protected override void RefreshCanPublish()
        {
            base.RefreshCanPublish();
            CanPublish = CanPublish
                && SelectedCluster != null
                && SelectedCluster != s_placeholderCluster;
        }

        private async Task OnRefreshClustersListCommand()
        {
            await RefreshClustersAsync();
        }

        private void OnCreateClusterCommand()
        {
            StartProcess(string.Format(GkeAddClusterUrlFormat, CredentialsStore.Default.CurrentProjectId));
        }

        #region IPublishDialogStep overrides

        public override FrameworkElement Content => _content;

        /// <summary>
        /// This step never goes next. <see cref="IPublishDialogStep.CanGoNext"/> is always <code>false</code>
        /// </summary>
        public override IPublishDialogStep Next()
        {
            throw new NotSupportedException();
        }

        protected override async Task InitializeDialogAsync()
        {
            // Start the task that initializes the dialog, mainly loads the GCP project.
            Task initializeDialogTask = base.InitializeDialogAsync();

            // In the meantime, set DeploymentName, which launches validations and updates the UI.
            DeploymentName = PublishDialog.Project.Name.ToLower();

            // Wait for the initialization task to be done.
            await initializeDialogTask;
        }

        protected override async Task ValidateProjectAsync()
        {
            RefreshClustersListCommand.CanExecuteCommand = false;
            CreateClusterCommand.CanExecuteCommand = false;

            await base.ValidateProjectAsync();
            if (IsValidGcpProject)
            {
                RefreshClustersListCommand.CanExecuteCommand = true;
                CreateClusterCommand.CanExecuteCommand = true;
            }
        }

        /// <inheritdoc />
        protected internal override IList<string> ApisRequieredForPublishing() => s_requiredApis;

        /// <summary>
        /// Clear Clusters from the previous selected project.
        /// </summary>
        protected override void ClearLoadedProjectData()
        {
            Clusters = Enumerable.Empty<Cluster>();
        }

        /// <summary>
        /// No project dependent data to load.
        /// </summary>
        /// <returns>A cached completed task.</returns>
        protected override Task LoadAnyProjectDataAsync() => TplExtensions.CompletedTask;

        /// <summary>
        /// Get clusters for the selected project.
        /// </summary>
        protected override async Task LoadValidProjectDataAsync()
        {
            await RefreshClustersAsync();
        }

        protected internal override void OnFlowFinished()
        {
            base.OnFlowFinished();
            _deploymentVersion = GcpPublishStepsUtils.GetDefaultVersion();
            _deploymentName = null;
            _replicas = ReplicasDefaultValue;
            RefreshClustersListCommand.CanExecuteCommand = false;
            CreateClusterCommand.CanExecuteCommand = false;
        }

        /// <summary>
        /// Start the publish operation.
        /// </summary>
        public override async void Publish()
        {
            IParsedProject project = PublishDialog.Project;
            try
            {
                ShellUtils.SaveAllFiles();

                Task<bool> verifyGCloudTask = GCloudWrapperUtils.VerifyGCloudDependencies(GCloudComponent.Kubectl);
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

                Task<KubectlContext> kubectlContextTask = GCloudWrapper.GetKubectlContextForClusterAsync(
                    cluster: SelectedCluster.Name,
                    zone: SelectedCluster.Zone,
                    context: gcloudContext);
                PublishDialog.TrackTask(kubectlContextTask);

                using (KubectlContext kubectlContext = await kubectlContextTask)
                {
                    Task<bool> deploymentExistsTask = KubectlWrapper.DeploymentExistsAsync(DeploymentName, kubectlContext);
                    PublishDialog.TrackTask(deploymentExistsTask);
                    if (await deploymentExistsTask)
                    {
                        if (!UserPromptUtils.ActionPrompt(
                                string.Format(Resources.GkePublishDeploymentAlreadyExistsMessage, DeploymentName),
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
                    GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishDeployingToGkeMessage, project.Name));

                    PublishDialog.FinishFlow();

                    TimeSpan deploymentDuration;
                    GkeDeploymentResult result;
                    using (StatusbarHelper.Freeze())
                    using (StatusbarHelper.ShowDeployAnimation())
                    using (ProgressBarHelper progress = StatusbarHelper.ShowProgressBar(Resources.GkePublishDeploymentStatusMessage))
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
                        OutputResultData(result, options, project);

                        StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                        if (OpenWebsite && result.ServiceExposed && result.PublicServiceIpAddress != null)
                        {
                            Process.Start($"http://{result.PublicServiceIpAddress}");
                        }

                        EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Success, deploymentDuration));
                    }
                    else
                    {
                        GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                        StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                        EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Failure));
                    }
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishDeploymentFailureMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                PublishDialog?.FinishFlow();

                EventsReporterWrapper.ReportEvent(GkeDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        #endregion

        private void OutputResultData(GkeDeploymentResult result, GkeDeployment.DeploymentOptions options, IParsedProject project)
        {
            GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishDeploymentSuccessMessage, project.Name));
            if (result.DeploymentUpdated)
            {
                GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishDeploymentUpdatedMessage, options.DeploymentName));
            }
            if (result.DeploymentScaled)
            {
                GcpOutputWindow.OutputLine(
                    string.Format(Resources.GkePublishDeploymentScaledMessage, options.DeploymentName, options.Replicas));
            }

            if (result.ServiceUpdated)
            {
                GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishServiceUpdatedMessage, options.DeploymentName));
            }
            if (result.ServiceExposed)
            {
                if (result.PublicServiceIpAddress != null)
                {
                    GcpOutputWindow.OutputLine(
                        string.Format(Resources.GkePublishServiceIpMessage, options.DeploymentName, result.PublicServiceIpAddress));
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
                            string.Format(
                                Resources.GkePublishServiceClusterIpMessage, options.DeploymentName, result.ClusterServiceIpAddress));
                    }
                }
            }
            if (result.ServiceDeleted)
            {
                GcpOutputWindow.OutputLine(string.Format(Resources.GkePublishServiceDeletedMessage, options.DeploymentName));
            }
        }

        private void InvalidateExposeService()
        {
            if (!ExposeService)
            {
                ExposePublicService = false;
                OpenWebsite = false;
            }
        }

        private async Task RefreshClustersAsync()
        {
            IList<Cluster> clusters = await CurrentDataSource.GetClusterListAsync();

            if (clusters == null || clusters.Count == 0)
            {
                Clusters = s_placeholderList;
            }
            else
            {
                Clusters = clusters.OrderBy(x => x.Name).ToList();
            }
        }

        /// <summary>
        /// Creates a GKE step complete with behavior and visuals.
        /// </summary>
        internal static GkeStepViewModel CreateStep(IGkeDataSource dataSource = null, IApiManager apiManager = null, Func<Project> pickProjectPrompt = null)
        {
            var content = new GkeStepContent();
            var viewModel = new GkeStepViewModel(content, dataSource, apiManager, pickProjectPrompt);
            content.DataContext = viewModel;

            return viewModel;
        }
    }
}