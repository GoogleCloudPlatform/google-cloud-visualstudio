﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
        private readonly FlexStepContent _content;
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
            CanPublish = true;
        }

        protected override void HasErrorsChanged()
        {
            CanPublish = !HasErrors;
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

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
                var verifyGcloudTask = GCloudWrapperUtils.VerifyGCloudDependencies();
                PublishDialog.TrackTask(verifyGcloudTask);
                if (!await verifyGcloudTask)
                {
                    Debug.WriteLine("Gcloud dependencies not met, aborting publish operation.");
                    return;
                }

                ShellUtils.SaveAllFiles();

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
        internal static FlexStepViewModel CreateStep()
        {
            var content = new FlexStepContent();
            var viewModel = new FlexStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
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
