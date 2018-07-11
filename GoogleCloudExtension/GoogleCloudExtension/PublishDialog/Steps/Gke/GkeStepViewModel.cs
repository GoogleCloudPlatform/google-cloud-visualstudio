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
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialog.Steps.Gke
{
    /// <summary>
    /// This class represents the deployment wizard's step to deploy an app to GKE.
    /// </summary>
    public class GkeStepViewModel : PublishDialogStepBase
    {
        public const string ClusterIdProjectPropertyName = "GoogleKubernetesEnginePublishClusterId";
        public const string DeploymentProjectPropertyName = "GoogleKubernetesEnginePublishDeploymentName";
        public const string VersionProjectPropertyName = "GoogleKubernetesEnginePublishVersion";
        public const string ReplicasProjectPropertyName = "GoogleKubernetesEnginePublishReplicas";
        public const string ExposeServiceProjectPropertyName = "GoogleKubernetesEnginePublishExposeService";
        public const string ExposePublicServiceProjectPropertyName = "GoogleKubernetesEnginePublishExposePublicService";
        public const string OpenWebsiteProjectPropertyName = "GoogleKubernetesEnginePublishOpenWebsite";

        internal static readonly Cluster s_placeholderCluster =
            new Cluster { Name = Resources.GkePublishNoClustersPlaceholder };

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

        private IEnumerable<Cluster> _clusters = Enumerable.Empty<Cluster>();
        private Cluster _selectedCluster = null;
        private string _deploymentName = null;
        private string _deploymentVersion = GcpPublishStepsUtils.GetDefaultVersion();
        private bool _exposeService = false;
        private bool _exposePublicService = false;
        private bool _openWebsite = false;
        private string _replicas = ReplicasDefaultValue;
        private string _lastClusterPropertyId;
        private AsyncProperty<IList<GkeDeployment>> _existingDeployments;
        private Task<IKubectlContext> _kubectlContext;
        internal readonly AsyncProperty<bool> _verifyGCloudTask;
        private GkeDeployment _selectedDeployment;
        private readonly IKubectlContextProvider _kubectlContextProvider;
        private readonly IBrowserService _browserService;
        private readonly Lazy<IDataSourceFactory> _dataSourceFactory;
        private readonly Lazy<IGkeDeploymentService> _deploymentService;

        /// <summary>
        /// List of APIs required for publishing to the current project.
        /// </summary>
        protected override IList<string> RequiredApis => s_requiredApis;

        /// <summary>
        /// The list of clusters that serve as the target for deployment.
        /// </summary>
        public IEnumerable<Cluster> Clusters
        {
            get => _clusters;
            private set
            {
                SelectedCluster = value?.FirstOrDefault(c => c.SelfLink == SelectedCluster?.SelfLink) ??
                    value?.FirstOrDefault(c => c.SelfLink == _lastClusterPropertyId) ?? value?.FirstOrDefault();
                SetValueAndRaise(ref _clusters, value);
            }
        }

        /// <summary>
        /// The currently selected cluster to use for deployment.
        /// </summary>
        public Cluster SelectedCluster
        {
            get => _selectedCluster;
            set
            {
                SetValueAndRaise(ref _selectedCluster, value);
                RefreshCanPublish();
                if (SelectedCluster != null && SelectedCluster != s_placeholderCluster)
                {
                    _kubectlContext = _kubectlContextProvider.GetKubectlContextForClusterAsync(SelectedCluster);
                    ExistingDeployments = new AsyncProperty<IList<GkeDeployment>>(GetDeploymentsAsync());
                }
            }
        }

        /// <summary>
        /// The list of deployments that already exist on the cluster.
        /// </summary>
        public AsyncProperty<IList<GkeDeployment>> ExistingDeployments
        {
            get => _existingDeployments;
            set
            {
                SetValueAndRaise(ref _existingDeployments, value);
                ExistingDeployments.PropertyChanged += (sender, args) => UpdateSelectedDeployment();
                UpdateSelectedDeployment();
            }
        }

        /// <summary>
        /// The selected existing deployment. Null if a new deployment is specified.
        /// </summary>
        public GkeDeployment SelectedDeployment
        {
            get => _selectedDeployment;
            set => SetValueAndRaise(ref _selectedDeployment, value);
        }

        /// <summary>
        /// The name to use for the deployment, by default the name of the project.
        /// </summary>
        public string DeploymentName
        {
            get => _deploymentName;
            set
            {
                IEnumerable<ValidationResult> validations =
                    GcpPublishStepsUtils.ValidateName(value, Resources.GkePublishDeploymentNameFieldName);
                SetAndRaiseWithValidation(ref _deploymentName, value, validations);
                UpdateSelectedDeployment();
            }
        }

        /// <summary>
        /// The version to use for the deployment, by default the date in a similar way to what gcloud uses for
        /// app engine.
        /// </summary>
        public string DeploymentVersion
        {
            get => _deploymentVersion;
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
            get => _replicas;
            set
            {
                IEnumerable<ValidationResult> validations =
                    GcpPublishStepsUtils.ValidatePositiveNonZeroInteger(value, Resources.GkePublishReplicasFieldName);
                SetAndRaiseWithValidation(ref _replicas, value, validations);
            }
        }

        /// <summary>
        /// Whether a service should be exposed for this deployment.
        /// </summary>
        public bool ExposeService
        {
            get => _exposeService;
            set
            {
                SetValueAndRaise(ref _exposeService, value);
                RaisePropertyChanged(nameof(ExposePublicService));
                RaisePropertyChanged(nameof(OpenWebsite));
            }
        }

        /// <summary>
        /// Whether the service to be exposed should be public on the internet or not.
        /// </summary>
        public bool ExposePublicService
        {
            get => ExposeService && _exposePublicService;
            set
            {
                SetValueAndRaise(ref _exposePublicService, value);
                RaisePropertyChanged(nameof(OpenWebsite));
            }
        }

        /// <summary>
        /// Whether the website should be open once a succesful deployment happens.
        /// </summary>
        public bool OpenWebsite
        {
            get => ExposeService && ExposePublicService && _openWebsite;
            set => SetValueAndRaise(ref _openWebsite, value);
        }

        /// <summary>
        /// Command to execute to create a new cluster.
        /// </summary>
        public ProtectedCommand CreateClusterCommand { get; }

        /// <summary>
        /// Command to execute to refresh the list of clusters.
        /// </summary>
        public ProtectedCommand RefreshClustersListCommand { get; }

        private IGkeDataSource DataSource => _dataSourceFactory.Value.CreateGkeDataSource();
        private IGkeDeploymentService DeploymentService => _deploymentService.Value;

        public GkeStepViewModel(IPublishDialog publishDialog) : base(publishDialog)
        {
            IGoogleCloudExtensionPackage package = GoogleCloudExtensionPackage.Instance;

            _dataSourceFactory = package.GetMefServiceLazy<IDataSourceFactory>();
            _kubectlContextProvider = package.GetMefService<IKubectlContextProvider>();
            _browserService = package.GetMefService<IBrowserService>();
            _deploymentService = package.GetMefServiceLazy<IGkeDeploymentService>();

            PublishCommandAsync = new ProtectedAsyncCommand(PublishAsync);
            CreateClusterCommand = new ProtectedCommand(OnCreateClusterCommand, false);
            RefreshClustersListCommand = new ProtectedCommand(OnRefreshClustersListCommand, false);
            _verifyGCloudTask =
                new AsyncProperty<bool>(GCloudWrapperUtils.VerifyGCloudDependenciesAsync(GCloudComponent.Kubectl));
            _verifyGCloudTask.PropertyChanged += (sender, args) => RefreshCanPublish();
        }

        /// <summary>
        /// Updates CanPublish from the step properties.
        /// </summary>
        protected override void RefreshCanPublish()
        {
            base.RefreshCanPublish();
            CanPublish = CanPublish &&
                SelectedCluster != null &&
                SelectedCluster != s_placeholderCluster &&
                _verifyGCloudTask.Value;
        }

        private async Task<IList<GkeDeployment>> GetDeploymentsAsync()
        {
            IKubectlContext context = await _kubectlContext;
            return await context.GetDeploymentsAsync();
        }

        private void UpdateSelectedDeployment()
        {
            if (SelectedDeployment?.Metadata.Name != DeploymentName)
            {
                SelectedDeployment = ExistingDeployments?.Value?.FirstOrDefault(d => d.Metadata.Name == DeploymentName);
            }
        }

        private void OnRefreshClustersListCommand() => PublishDialog.TrackTask(RefreshClustersAsync());

        private void OnCreateClusterCommand() => _browserService.OpenBrowser(
            string.Format(GkeAddClusterUrlFormat, CredentialsStore.Default.CurrentProjectId));

        protected internal override ProtectedAsyncCommand PublishCommandAsync { get; }

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

        /// <summary>
        /// Clear Clusters from the previous selected project.
        /// </summary>
        protected override void ClearLoadedProjectData() => Clusters = Enumerable.Empty<Cluster>();

        /// <summary>
        /// No project dependent data to load.
        /// </summary>
        /// <returns>A cached completed task.</returns>
        protected override Task LoadAnyProjectDataAsync() => Task.CompletedTask;

        /// <summary>
        /// Get clusters for the selected project.
        /// </summary>
        protected override async Task LoadValidProjectDataAsync() => await RefreshClustersAsync();

        protected internal override void OnFlowFinished()
        {
            base.OnFlowFinished();
            _deploymentVersion = GcpPublishStepsUtils.GetDefaultVersion();
            _deploymentName = null;
            _replicas = ReplicasDefaultValue;
            RefreshClustersListCommand.CanExecuteCommand = false;
            CreateClusterCommand.CanExecuteCommand = false;
        }

        protected override void LoadProjectProperties()
        {
            _lastClusterPropertyId = PublishDialog.Project.GetUserProperty(ClusterIdProjectPropertyName);

            string deploymentName = PublishDialog.Project.GetUserProperty(DeploymentProjectPropertyName);
            if (string.IsNullOrWhiteSpace(deploymentName))
            {
                DeploymentName = GcpPublishStepsUtils.ToValidName(PublishDialog.Project.Name);
            }
            else
            {
                DeploymentName = deploymentName;
            }

            string version = PublishDialog.Project.GetUserProperty(VersionProjectPropertyName);
            if (!string.IsNullOrWhiteSpace(version))
            {
                DeploymentVersion = version;
            }

            string replicas = PublishDialog.Project.GetUserProperty(ReplicasProjectPropertyName);
            if (!string.IsNullOrWhiteSpace(replicas))
            {
                Replicas = replicas;
            }

            string exposeServiceProperty = PublishDialog.Project.GetUserProperty(ExposeServiceProjectPropertyName);
            if (bool.TryParse(exposeServiceProperty, out bool exposeService))
            {
                ExposeService = exposeService;
            }

            string exposePublicServiceProperty =
                PublishDialog.Project.GetUserProperty(ExposePublicServiceProjectPropertyName);
            if (bool.TryParse(exposePublicServiceProperty, out bool exposePublicService))
            {
                ExposePublicService = exposePublicService;
            }

            string openWebsiteProperty = PublishDialog.Project.GetUserProperty(OpenWebsiteProjectPropertyName);
            if (bool.TryParse(openWebsiteProperty, out bool openWebsite))
            {
                OpenWebsite = openWebsite;
            }
        }

        protected override void SaveProjectProperties()
        {
            PublishDialog.Project.SaveUserProperty(ClusterIdProjectPropertyName, SelectedCluster?.SelfLink);
            if (!PropertyHasErrors(nameof(DeploymentName)))
            {
                PublishDialog.Project.SaveUserProperty(DeploymentProjectPropertyName, DeploymentName);
            }

            if (!PropertyHasErrors(nameof(DeploymentVersion)))
            {
                PublishDialog.Project.SaveUserProperty(VersionProjectPropertyName, DeploymentVersion);
            }

            if (!PropertyHasErrors(nameof(Replicas)))
            {
                PublishDialog.Project.SaveUserProperty(ReplicasProjectPropertyName, Replicas);
            }

            PublishDialog.Project.SaveUserProperty(ExposeServiceProjectPropertyName, ExposeService.ToString());

            // Use fields directly here rather than Properties to save hidden state.
            PublishDialog.Project.SaveUserProperty(
                ExposePublicServiceProjectPropertyName, _exposePublicService.ToString());
            PublishDialog.Project.SaveUserProperty(OpenWebsiteProjectPropertyName, _openWebsite.ToString());
        }

        /// <summary>
        /// Start the publish operation.
        /// </summary>
        private async Task PublishAsync()
        {

            PublishDialog.TrackTask(_kubectlContext);

            using (IKubectlContext kubectlContext = await _kubectlContext)
            {
                var options = new GkeDeploymentService.Options(
                    kubectlContext,
                    DeploymentName,
                    DeploymentVersion,
                    SelectedDeployment,
                    ExposeService,
                    ExposePublicService,
                    OpenWebsite,
                    SelectedConfiguration,
                    int.Parse(Replicas));

                DeploymentVersion = GcpPublishStepsUtils.IncrementVersion(DeploymentVersion);

                PublishDialog.FinishFlow();

                await DeploymentService.DeployProjectToGkeAsync(PublishDialog.Project, options);
            }
        }

        private async Task RefreshClustersAsync()
        {
            IList<Cluster> clusters = await DataSource.GetClusterListAsync();

            if (clusters == null || clusters.Count == 0)
            {
                Clusters = s_placeholderList;
            }
            else
            {
                Clusters = clusters.OrderBy(x => x.Name).ToList();
            }
        }
    }
}