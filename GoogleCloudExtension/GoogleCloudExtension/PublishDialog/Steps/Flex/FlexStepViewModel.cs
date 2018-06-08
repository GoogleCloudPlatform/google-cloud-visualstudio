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

using Google.Apis.Appengine.v1.Data;
using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services.Configuration;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialog.Steps.Flex
{
    /// <summary>
    /// The view model for the Flex step in the publish app wizard.
    /// </summary>
    public class FlexStepViewModel : PublishDialogStepBase
    {
        public const string PromoteProjectPropertyName = "GoogleAppEnginePublishPromote";
        public const string OpenWebsiteProjectPropertyName = "GoogleAppEnginePublishOpenWebsite";
        public const string NextVersionProjectPropertyName = "GoogleAppEnginePublishNextVersion";

        // The list of APIs that are required for a successful deployment to App Engine Flex.
        private static readonly IList<string> s_requiredApis = new List<string>
        {
            // We require the App Engine Admin API in order to deploy to app engine.
            KnownApis.AppEngineAdminApiName
        };

        private readonly IGaeDataSource _dataSource;
        private readonly Func<Task<bool>> _setAppRegionAsyncFunc;
        private string _version = GcpPublishStepsUtils.GetDefaultVersion();
        private bool _promote = true;
        private bool _openWebsite = true;
        private bool _needsAppCreated = false;
        private string _service;
        private readonly Lazy<IAppEngineFlexDeployment> _deploymentService;
        private readonly Lazy<IAppEngineConfiguration> _configurationService;
        private bool _updateAppYamlService;
        private bool _updateAppYamlServiceEnabled;
        private IList<string> _services = new List<string>();

        private Func<Task<bool>> SetAppRegionAsyncFunc => _setAppRegionAsyncFunc ??
            (() => GaeUtils.SetAppRegionAsync(CredentialsStore.Default.CurrentProjectId, CurrentDataSource));

        /// <summary>
        /// List of APIs required for publishing to the current project.
        /// </summary>
        protected override IList<string> RequiredApis => s_requiredApis;

        /// <summary>
        /// The version to use for the the app in App Engine Flex.
        /// </summary>
        public string Version
        {
            get { return _version; }
            set
            {
                IEnumerable<ValidationResult> validations =
                    GcpPublishStepsUtils.ValidateName(value, Resources.PublishDialogFlexVersionNameFieldName);
                SetAndRaiseWithValidation(ref _version, value, validations);
            }
        }

        /// <summary>
        /// Whether to promote the app or not. Default to true.
        /// </summary>
        public bool Promote
        {
            get { return _promote; }
            set { SetValueAndRaise(ref _promote, value); }
        }

        /// <summary>
        /// Whether to open the webiste at the end of a succesfull publish process. Default to true.
        /// </summary>
        public bool OpenWebsite
        {
            get { return _openWebsite; }
            set { SetValueAndRaise(ref _openWebsite, value); }
        }

        /// <summary>
        /// Whether the GCP project selected needs the App Engine app created, and the region set, before
        /// a deployment can be made.
        /// </summary>
        public bool NeedsAppCreated
        {
            get { return _needsAppCreated; }
            set
            {
                SetValueAndRaise(ref _needsAppCreated, value);
                SetAppRegionCommand.CanExecuteCommand = value;
                RaisePropertyChanged(nameof(ShowInputControls));
            }
        }

        /// <summary>
        /// Whether to display the input controls to the user.
        /// </summary>
        public override bool ShowInputControls => base.ShowInputControls && !NeedsAppCreated;

        public IList<string> Services
        {
            get => _services;
            set => SetValueAndRaise(ref _services, value);
        }

        /// <summary>
        /// The name of the service to deploy to.
        /// </summary>
        public string Service
        {
            get => _service;
            set
            {
                IEnumerable<ValidationResult> validations =
                    GcpPublishStepsUtils.ValidateServiceName(value, Resources.PublishDialogFlexServiceName);
                SetAndRaiseWithValidation(ref _service, value, validations);
                UpdateAppYamlServiceEnabled =
                    Service != ConfigurationService.GetAppEngineService(PublishDialog.Project);
            }
        }

        /// <summary>
        /// If true, will update the Service config in app.yaml.
        /// </summary>
        public bool UpdateAppYamlService
        {
            get => UpdateAppYamlServiceEnabled && _updateAppYamlService;
            set => SetValueAndRaise(ref _updateAppYamlService, value);
        }

        /// <summary>
        /// Controls whether the Update app.yaml checkbox is enabled.
        /// </summary>
        public bool UpdateAppYamlServiceEnabled
        {
            get => _updateAppYamlServiceEnabled;
            set
            {
                SetValueAndRaise(ref _updateAppYamlServiceEnabled, value);
                RaisePropertyChanged(nameof(UpdateAppYamlService));
            }
        }

        /// <summary>
        /// The command to execute to create the App Engine app and set the region for it.
        /// </summary>
        public ProtectedAsyncCommand SetAppRegionCommand { get; }

        public override IProtectedCommand PublishCommand => PublishCommandAsync;

        public ProtectedAsyncCommand PublishCommandAsync { get; }

        private IGaeDataSource CurrentDataSource => _dataSource ?? new GaeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.Instance.ApplicationName);

        private IAppEngineFlexDeployment DeploymentService => _deploymentService.Value;
        private IAppEngineConfiguration ConfigurationService => _configurationService.Value;

        public FlexStepViewModel(
            IGaeDataSource dataSource,
            IApiManager apiManager,
            Func<Project> pickProjectPrompt,
            Func<Task<bool>> setAppRegionAsyncFunc,
            IPublishDialog publishDialog)
            : base(apiManager, pickProjectPrompt, publishDialog)
        {
            _dataSource = dataSource;
            _setAppRegionAsyncFunc = setAppRegionAsyncFunc;

            SetAppRegionCommand = new ProtectedAsyncCommand(OnSetAppRegionCommandAsync, false);

            PublishCommandAsync = new ProtectedAsyncCommand(PublishAsync);
            _deploymentService = GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IAppEngineFlexDeployment>();
            _configurationService = GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IAppEngineConfiguration>();
        }

        protected internal override void OnFlowFinished()
        {
            base.OnFlowFinished();
            NeedsAppCreated = false;
        }

        #region IPublishDialogStep

        protected override async Task ValidateProjectAsync()
        {
            NeedsAppCreated = false;

            await base.ValidateProjectAsync();

            if (IsValidGcpProject)
            {
                // Using the GAE API, check if there's an app for the project.
                if (null == await CurrentDataSource.GetApplicationAsync())
                {
                    Debug.WriteLine("Needs App created.");
                    NeedsAppCreated = true;
                    IsValidGcpProject = false;
                }
            }
        }

        /// <summary>
        /// No project dependent data to clear.
        /// </summary>
        protected override void ClearLoadedProjectData() { }

        /// <summary>
        /// No project dependent data to load.
        /// </summary>
        /// <returns>A cached completed task.</returns>
        protected override Task LoadAnyProjectDataAsync() => Task.CompletedTask;

        /// <summary>
        /// No project dependent data to load.
        /// </summary>
        /// <returns>A cached completed task.</returns>
        protected override async Task LoadValidProjectDataAsync()
        {
            IList<Service> services = await CurrentDataSource.GetServiceListAsync();
            Services = services?.Select(s => s.Id).ToList();
        }

        private async Task OnSetAppRegionCommandAsync()
        {
            Task<bool> setAppRegionTask = SetAppRegionAsyncFunc();
            PublishDialog.TrackTask(setAppRegionTask);
            if (await setAppRegionTask)
            {
                LoadProject();
            }
        }

        private async Task PublishAsync()
        {
            Task<bool> verifyGcloudTask = GCloudWrapperUtils.VerifyGCloudDependencies();
            PublishDialog.TrackTask(verifyGcloudTask);
            if (!await verifyGcloudTask)
            {
                return;
            }

            var options = new AppEngineFlexDeployment.DeploymentOptions(Service, Version, Promote, OpenWebsite);
            Version = GcpPublishStepsUtils.IncrementVersion(Version);

            PublishDialog.FinishFlow();

            await DeploymentService.PublishProjectAsync(PublishDialog.Project, options);
        }

        protected override void LoadProjectProperties()
        {
            string promoteProperty = PublishDialog.Project.GetUserProperty(PromoteProjectPropertyName);
            if (bool.TryParse(promoteProperty, out bool promote))
            {
                Promote = promote;
            }

            string openWebsiteProperty = PublishDialog.Project.GetUserProperty(OpenWebsiteProjectPropertyName);
            if (bool.TryParse(openWebsiteProperty, out bool openWebSite))
            {
                OpenWebsite = openWebSite;
            }

            string nextVersionProperty = PublishDialog.Project.GetUserProperty(NextVersionProjectPropertyName);
            if (!string.IsNullOrWhiteSpace(nextVersionProperty))
            {
                Version = nextVersionProperty;
            }
            else
            {
                Version = GcpPublishStepsUtils.GetDefaultVersion();
            }

            Service = ConfigurationService.GetAppEngineService(PublishDialog.Project);
        }

        protected override void SaveProjectProperties()
        {
            PublishDialog.Project.SaveUserProperty(PromoteProjectPropertyName, Promote.ToString());
            PublishDialog.Project.SaveUserProperty(OpenWebsiteProjectPropertyName, OpenWebsite.ToString());
            if (string.IsNullOrWhiteSpace(Version) || GcpPublishStepsUtils.IsDefaultVersion(Version))
            {
                PublishDialog.Project.DeleteUserProperty(NextVersionProjectPropertyName);
            }
            else
            {
                PublishDialog.Project.SaveUserProperty(NextVersionProjectPropertyName, Version);
            }

            if (UpdateAppYamlService)
            {
                ConfigurationService.SaveServiceToAppYaml(PublishDialog.Project, Service);
            }
        }

        #endregion
    }
}
