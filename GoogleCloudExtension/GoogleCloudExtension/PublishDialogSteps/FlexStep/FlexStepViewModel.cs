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

namespace GoogleCloudExtension.PublishDialogSteps.FlexStep
{
    /// <summary>
    /// The view model for the Flex step in the publish app wizard.
    /// </summary>
    public class FlexStepViewModel : PublishDialogStepBase
    {
        // The list of APIs that are required for a successful deployment to App Engine Flex.
        private static readonly IEnumerable<string> s_requiredApis = new List<string>
        {
            // We require the App Engine Admin API in order to deploy to app engine.
            KnownApis.AppEngineAdminApiName,
        };

        private readonly FlexStepContent _content;
        private readonly Task<bool> _projectStateValidation;
        private IPublishDialog _publishDialog;
        private string _version = GcpPublishStepsUtils.GetDefaultVersion();
        private bool _promote = true;
        private bool _openWebsite = true;

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

        private FlexStepViewModel(FlexStepContent content)
        {
            _content = content;
            _projectStateValidation = ValidatGcpeProjectState();
            CanPublish = true;
        }

        protected override void HasErrorsChanged()
        {
            CanPublish = !HasErrors;
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        public override async void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;

            _publishDialog.TrackTask(_projectStateValidation);

            if (!await _projectStateValidation)
            {
                // Close the dialog if the project cannot be deployed.
                _publishDialog.FinishFlow();
            }
        }

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

                var verifyGcloudTask = GCloudWrapperUtils.VerifyGCloudDependencies();
                _publishDialog.TrackTask(verifyGcloudTask);
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
                GcpOutputWindow.OutputLine(String.Format(Resources.GcePublishStepStartMessage, project.Name));

                _publishDialog.FinishFlow();

                TimeSpan deploymentDuration;
                AppEngineFlexDeploymentResult result;
                using (StatusbarHelper.Freeze())
                using (StatusbarHelper.ShowDeployAnimation())
                using (var progress = StatusbarHelper.ShowProgressBar(Resources.FlexPublishProgressMessage))
                using (ShellUtils.SetShellUIBusy())
                {
                    var startDeploymentTime = DateTime.Now;
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
                    GcpOutputWindow.OutputLine(String.Format(Resources.FlexPublishSuccessMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                    var url = result.GetDeploymentUrl();
                    GcpOutputWindow.OutputLine(String.Format(Resources.PublishUrlMessage, url));
                    if (OpenWebsite)
                    {
                        Process.Start(url);
                    }

                    EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Success, deploymentDuration));
                }
                else
                {
                    GcpOutputWindow.OutputLine(String.Format(Resources.FlexPublishFailedMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Failure));
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.FlexPublishFailedMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                EventsReporterWrapper.ReportEvent(GaeDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        private bool ValidateInput()
        {
            if (String.IsNullOrEmpty(Version))
            {
                UserPromptUtils.ErrorPrompt(Resources.FlexPublishEmptyVersionMessage, Resources.UiInvalidValueTitle);
                return false;
            }
            if (!GcpPublishStepsUtils.IsValidName(Version))
            {
                UserPromptUtils.ErrorPrompt(
                    String.Format(Resources.FlexPublishInvalidVersionMessage, Version),
                    Resources.UiInvalidValueTitle);
                return false;
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Creates a new step instance. This method will also create the necessary view and conect both
        /// objects together.
        /// </summary>
        internal static FlexStepViewModel CreateStep()
        {
            var content = new FlexStepContent();
            var viewModel = new FlexStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }

        private async Task<bool> ValidatGcpeProjectState()
        {
            // Ensure the necessary APIs are enabled.
            if (!await ApiManager.Default.EnsureAllServicesEnabledAsync(
                    s_requiredApis,
                    Resources.FlexPublishEnableApiMessage))
            {
                Debug.WriteLine($"The user refused to enable the APIs for GAE.");
                return false;
            }

            try
            {
                // Using the GAE API, check if there's an app for the project.
                var appEngineDataSource = new GaeDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.ApplicationName);
                var app = await appEngineDataSource.GetApplicationAsync();
                if (app == null)
                {
                    Debug.WriteLine("There's no App Engine app for the project.");
                    UserPromptUtils.ErrorPrompt(
                        message: Resources.FlexPublishNoAppFoundMessage,
                        title: Resources.UiErrorCaption);
                    return false;
                }

                // The project is ready to go.
                return true;
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ExceptionPrompt(ex);
                return false;
            }
        }
    }
}
