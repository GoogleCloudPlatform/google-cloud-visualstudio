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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog.Steps.Gce
{
    /// <summary>
    /// The view model for the publish step that publishes the project to a GCE VM.
    /// </summary>
    public class GceStepViewModel : PublishDialogStepBase
    {
        // The list of APIs that are required for a succesful deployment to GCE.
        private static readonly IList<string> s_requiredApis = new List<string>
        {
            // Need the GCE API to perform all work.
            KnownApis.ComputeEngineApiName
        };

        private readonly IGceDataSource _dataSource;
        private readonly IWindowsCredentialsStore _currentWindowsCredentialStore;
        private readonly Action<Instance> _manageCredentialsPrompt;
        private Instance _selectedInstance = null;
        private IEnumerable<WindowsInstanceCredentials> _credentials = Enumerable.Empty<WindowsInstanceCredentials>();
        private WindowsInstanceCredentials _selectedCredentials = null;
        private bool _openWebsite = true;
        private bool _launchRemoteDebugger = false;
        private IEnumerable<Instance> _instances = Enumerable.Empty<Instance>();

        /// <summary>
        /// List of APIs required for publishing to the current project.
        /// </summary>
        protected override IList<string> RequiredApis => s_requiredApis;

        /// <summary>
        /// The asynchrnous value that will resolve to the list of instances in the current GCP Project, and that are
        /// the available target for the publish process.
        /// </summary>
        public IEnumerable<Instance> Instances
        {
            get { return _instances; }
            private set
            {
                SetValueAndRaise(ref _instances, value);
                SelectedInstance = value?.FirstOrDefault();
            }
        }

        /// <summary>
        /// The selected GCE VM that will be the target of the publish process.
        /// </summary>
        public Instance SelectedInstance
        {
            get { return _selectedInstance; }
            set
            {
                SetValueAndRaise(ref _selectedInstance, value);
                UpdateCredentials();
                ManageCredentialsCommand.CanExecuteCommand = value != null;
            }
        }

        /// <summary>
        /// The list of credentials available for the selected <seealso cref="Instance"/>.
        /// </summary>
        public IEnumerable<WindowsInstanceCredentials> Credentials
        {
            get { return _credentials; }
            private set
            {
                SetValueAndRaise(ref _credentials, value);
                SelectedCredentials = value?.FirstOrDefault();
            }
        }

        /// <summary>
        /// The selected <seealso cref="WindowsInstanceCredentials"/> to use for the publish process.
        /// </summary>
        public WindowsInstanceCredentials SelectedCredentials
        {
            get { return _selectedCredentials; }
            set
            {
                SetValueAndRaise(ref _selectedCredentials, value);
                RefreshCanPublish();
            }
        }

        /// <summary>
        /// The command to execute when pressing the manage credentials button.
        /// </summary>
        public ProtectedCommand ManageCredentialsCommand { get; }

        public ProtectedCommand RefreshInstancesCommand { get; }

        /// <summary>
        /// Whether to open the website after a succesful publish operation. Defaults to true.
        /// </summary>
        public bool OpenWebsite
        {
            get { return _openWebsite; }
            set { SetValueAndRaise(ref _openWebsite, value); }
        }

        /// <summary>
        /// Whether to attach debugger after publising.
        /// </summary>
        public bool LaunchRemoteDebugger
        {
            get { return _launchRemoteDebugger; }
            set { SetValueAndRaise(ref _launchRemoteDebugger, value); }
        }

        private IGceDataSource CurrentDataSource =>
            _dataSource ?? new GceDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);

        private IWindowsCredentialsStore CurrentWindowsCredentialsStore =>
            _currentWindowsCredentialStore ?? WindowsCredentialsStore.Default;

        private Action<Instance> ManageCredentialsPrompt =>
            _manageCredentialsPrompt ?? ManageWindowsCredentialsWindow.PromptUser;

        public GceStepViewModel(
            IGceDataSource dataSource,
            IApiManager apiManager,
            Func<Google.Apis.CloudResourceManager.v1.Data.Project> pickProjectPrompt,
            IWindowsCredentialsStore currentWindowsCredentialStore,
            Action<Instance> manageCredentialsPrompt,
            IPublishDialog publishDialog)
            : base(apiManager, pickProjectPrompt, publishDialog)
        {
            _dataSource = dataSource;
            _currentWindowsCredentialStore = currentWindowsCredentialStore;
            _manageCredentialsPrompt = manageCredentialsPrompt;

            ManageCredentialsCommand = new ProtectedCommand(OnManageCredentialsCommand, false);
            RefreshInstancesCommand = new ProtectedCommand(
                () => PublishDialog.TrackTask(LoadValidProjectDataAsync()), false);
            PublishCommand = new ProtectedAsyncCommand(PublishAsync);
        }

        public override IProtectedCommand PublishCommand { get; }

        protected override void OnIsValidGcpProjectChanged()
        {
            RefreshInstancesCommand.CanExecuteCommand = IsValidGcpProject;
        }

        private void OnManageCredentialsCommand()
        {
            ManageCredentialsPrompt(SelectedInstance);
            UpdateCredentials();
        }

        #region IPublishDialogStep

        private async Task PublishAsync()
        {
            IParsedProject project = PublishDialog.Project;
            Instance selectedInstance = SelectedInstance;
            WindowsInstanceCredentials selectedCredentials = SelectedCredentials;

            try
            {
                ShellUtils.SaveAllFiles();

                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine(string.Format(Resources.GcePublishStepStartMessage, project.Name));

                PublishDialog.FinishFlow();

                string progressBarTitle = string.Format(Resources.GcePublishProgressMessage, selectedInstance.Name);
                TimeSpan deploymentDuration;
                bool result;
                using (StatusbarHelper.Freeze())
                using (StatusbarHelper.ShowDeployAnimation())
                using (ShellUtils.SetShellUIBusy())
                using (ProgressBarHelper progress = StatusbarHelper.ShowProgressBar(progressBarTitle))
                {
                    DateTime startDeploymentTime = DateTime.Now;
                    result = await WindowsVmDeployment.PublishProjectAsync(
                        project,
                        selectedInstance,
                        selectedCredentials,
                        progress,
                        VsVersionUtils.ToolsPathProvider,
                        GcpOutputWindow.OutputLine);
                    deploymentDuration = DateTime.Now - startDeploymentTime;
                }

                if (result)
                {
                    GcpOutputWindow.OutputLine(string.Format(Resources.GcePublishSuccessMessage, project.Name, selectedInstance.Name));
                    StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                    string url = selectedInstance.GetDestinationAppUri();
                    GcpOutputWindow.OutputLine(string.Format(Resources.PublishUrlMessage, url));
                    if (OpenWebsite)
                    {
                        Process.Start(url);
                    }

                    EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Success, deploymentDuration));

                    if (LaunchRemoteDebugger)
                    {
                        AttachDebuggerDialog.AttachDebuggerWindow.PromptUser(selectedInstance);
                    }
                }
                else
                {
                    GcpOutputWindow.OutputLine(string.Format(Resources.GcePublishFailedMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Failure));
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(string.Format(Resources.GcePublishFailedMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                PublishDialog?.FinishFlow();

                EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        /// <summary>
        /// Clearing instances from a potential previous project.
        /// </summary>
        protected override void ClearLoadedProjectData()
        {
            Instances = Enumerable.Empty<Instance>();
        }

        /// <summary>
        /// No data to load
        /// </summary>
        /// <returns>A cached completed task</returns>
        protected override Task LoadAnyProjectDataAsync() => Task.CompletedTask;

        /// <summary>
        /// Loads the instances of the project given that it is valid.
        /// </summary>
        protected override async Task LoadValidProjectDataAsync()
        {
            IList<Instance> instances = await CurrentDataSource.GetInstanceListAsync();
            Instances = instances.Where(x => x.IsRunning() && x.IsWindowsInstance()).OrderBy(x => x.Name);
        }

        protected override void RefreshCanPublish()
        {
            base.RefreshCanPublish();
            CanPublish = CanPublish
                && SelectedCredentials != null;
        }

        #endregion

        private void UpdateCredentials()
        {
            if (SelectedInstance == null)
            {
                Credentials = Enumerable.Empty<WindowsInstanceCredentials>();
            }
            else
            {
                Credentials = CurrentWindowsCredentialsStore.GetCredentialsForInstance(SelectedInstance);
            }
        }
    }
}
