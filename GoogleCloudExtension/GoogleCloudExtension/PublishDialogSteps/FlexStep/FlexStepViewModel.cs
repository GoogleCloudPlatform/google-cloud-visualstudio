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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.PublishDialogSteps.FlexStep
{
    /// <summary>
    /// The view model for the Flex step in the publish app wizard.
    /// </summary>
    public class FlexStepViewModel : PublishDialogStepBase
    {
        // The list of APIs that are required for a successful deployment to App Engine Flex.
        private static readonly IList<string> s_requiredApis = new List<string>
        {
            // We require the App Engine Admin API in order to deploy to app engine.
            KnownApis.AppEngineAdminApiName,
        };

        private readonly FlexStepContent _content;
        private readonly IGaeDataSource _dataSource = null;
        private string _version = GcpPublishStepsUtils.GetDefaultVersion();
        private bool _promote = true;
        private bool _openWebsite = true;
        private bool _needsAppCreated = false;

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
                RaisePropertyChanged(nameof(ShowInputControls));
            }
        }

        /// <summary>
        /// Whether to display the input controls to the user.
        /// </summary>
        public override bool ShowInputControls => base.ShowInputControls && !NeedsAppCreated;

        /// <summary>
        /// The command to execute to enable the necessary APIs for the project.
        /// </summary>
        public ICommand EnableApiCommand { get; }

        /// <summary>
        /// The command to execute to create the App Engine app and set the region for it.
        /// </summary>
        public ICommand SetAppRegionCommand { get; }

        private IGaeDataSource CurrentDataSource => _dataSource ?? new GaeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);

        private FlexStepViewModel(FlexStepContent content, IGaeDataSource dataSource = null, IApiManager apiManager = null)
            : base(apiManager)
        {
            _content = content;
            _dataSource = dataSource;

            EnableApiCommand = new ProtectedAsyncCommand(OnEnableApiCommandAsync);
            SetAppRegionCommand = new ProtectedAsyncCommand(OnSetAppRegionCommandAsync);
        }

        private async Task OnSetAppRegionCommandAsync()
        {
            if (await GaeUtils.SetAppRegionAsync(CredentialsStore.Default.CurrentProjectId, CurrentDataSource))
            {
                PublishDialog.TrackTask(ValidateGcpProjectState());
            }
        }

        private async Task OnEnableApiCommandAsync()
        {
            await CurrentApiManager.EnableServicesAsync(s_requiredApis);
            PublishDialog.TrackTask(ValidateGcpProjectState());
        }

        protected override void HasErrorsChanged()
        {
            CanPublish = !HasErrors;
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            base.OnPushedToDialog(dialog);

            InitializeDialogState();
        }

        public override async void Publish()
        {
            if (!ValidateInput())
            {
                Debug.WriteLine("Invalid input cancelled the operation.");
                return;
            }

            IParsedProject project = PublishDialog.Project;
            try
            {
                ShellUtils.SaveAllFiles();

                var verifyGcloudTask = GCloudWrapperUtils.VerifyGCloudDependencies();
                PublishDialog.TrackTask(verifyGcloudTask);
                if (!await verifyGcloudTask)
                {
                    Debug.WriteLine("Gcloud dependencies not met, aborting publish operation.");
                    return;
                }

                var context = new GCloudContext
                {
                    CredentialsPath = CredentialsStore.Default.CurrentAccountPath,
                    ProjectId = CredentialsStore.Default.CurrentProjectId,
                    AppName = GoogleCloudExtensionPackage.ApplicationName,
                    AppVersion = GoogleCloudExtensionPackage.ApplicationVersion,
                };
                var options = new AppEngineFlexDeployment.DeploymentOptions
                {
                    Version = Version,
                    Promote = Promote,
                    Context = context
                };

                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine(string.Format(Resources.GcePublishStepStartMessage, project.Name));

                PublishDialog.FinishFlow();

                TimeSpan deploymentDuration;
                AppEngineFlexDeploymentResult result;
                using (StatusbarHelper.Freeze())
                using (StatusbarHelper.ShowDeployAnimation())
                using (ProgressBarHelper progress =
                    StatusbarHelper.ShowProgressBar(Resources.FlexPublishProgressMessage))
                using (ShellUtils.SetShellUIBusy())
                {
                    DateTime startDeploymentTime = DateTime.Now;
                    result = await AppEngineFlexDeployment.PublishProjectAsync(
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
                    if (OpenWebsite)
                    {
                        Process.Start(url);
                    }

                    EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Success, deploymentDuration));
                }
                else
                {
                    GcpOutputWindow.OutputLine(string.Format(Resources.FlexPublishFailedMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Failure));
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(string.Format(Resources.FlexPublishFailedMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        #endregion

        /// <summary>
        /// Creates a new step instance. This method will also create the necessary view and conect both
        /// objects together.
        /// </summary>
        internal static FlexStepViewModel CreateStep(IGaeDataSource dataSource = null, IApiManager apiManager = null)
        {
            var content = new FlexStepContent();
            var viewModel = new FlexStepViewModel(content, dataSource: dataSource, apiManager: apiManager);
            content.DataContext = viewModel;

            return viewModel;
        }

        /// <summary>
        /// If the user changes the current project we need to re-run the validation to make sure that the
        /// selected project has the right APIs enabled.
        /// </summary>
        protected override void OnProjectChanged()
        {
            InitializeDialogState();
        }

        private void InitializeDialogState()
        {
            LoadingProjectTask = ValidateGcpProjectState();
            PublishDialog.TrackTask(LoadingProjectTask);
        }

        private async Task ValidateGcpProjectState()
        {
            try
            {
                // Go into loading mode.
                LoadingProject = true;

                // Clean slate for the messages.
                CanPublish = true;
                NeedsApiEnabled = false;
                NeedsAppCreated = false;
                GeneralError = false;

                // Ensure the necessary APIs are enabled.
                if (!await CurrentApiManager.AreServicesEnabledAsync(s_requiredApis))
                {
                    Debug.WriteLine("The user refused to enable the APIs for GAE.");
                    CanPublish = false;
                    NeedsApiEnabled = true;
                    return;
                }

                // Using the GAE API, check if there's an app for the project.
                Google.Apis.Appengine.v1.Data.Application app = await CurrentDataSource.GetApplicationAsync();
                if (app == null)
                {
                    CanPublish = false;
                    NeedsAppCreated = true;
                    return;
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                CanPublish = false;
                GeneralError = true;
            }
            finally
            {
                LoadingProject = false;
            }
        }

        internal bool ValidateInput()
        {
            if (string.IsNullOrEmpty(Version))
            {
                UserPromptUtils.ErrorPrompt(Resources.FlexPublishEmptyVersionMessage, Resources.UiInvalidValueTitle);
                return false;
            }
            if (!GcpPublishStepsUtils.IsValidName(Version))
            {
                UserPromptUtils.ErrorPrompt(
                    string.Format(Resources.FlexPublishInvalidVersionMessage, Version),
                    Resources.UiInvalidValueTitle);
                return false;
            }

            return true;
        }
    }
}
