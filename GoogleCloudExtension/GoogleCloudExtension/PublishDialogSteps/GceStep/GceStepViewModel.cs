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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.GceStep
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
            KnownApis.ComputeEngineApiName,
        };

        private readonly GceStepContent _content;
        private readonly IGceDataSource _dataSource;
        private readonly IWindowsCredentialsStore _currentWindowsCredentialStore;
        private readonly Action<Instance> _manageCredentialsPrompt;
        private Instance _selectedInstance;
        private IEnumerable<WindowsInstanceCredentials> _credentials;
        private WindowsInstanceCredentials _selectedCredentials;
        private bool _openWebsite = true;
        private bool _launchRemoteDebugger;
        private IEnumerable<Instance> _instances;

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

        private GceStepViewModel(
            GceStepContent content,
            IGceDataSource dataSource,
            IApiManager apiManager,
            Func<Google.Apis.CloudResourceManager.v1.Data.Project> pickProjectPrompt,
            IWindowsCredentialsStore currentWindowsCredentialStore,
            Action<Instance> manageCredentialsPrompt)
            : base(apiManager, pickProjectPrompt)
        {
            _content = content;
            _dataSource = dataSource;
            _currentWindowsCredentialStore = currentWindowsCredentialStore;
            _manageCredentialsPrompt = manageCredentialsPrompt;

            _instances = Enumerable.Empty<Instance>();
            _credentials = Enumerable.Empty<WindowsInstanceCredentials>();

            ManageCredentialsCommand = new ProtectedCommand(OnManageCredentialsCommand, canExecuteCommand: false);
        }

        private void OnManageCredentialsCommand()
        {
            ManageCredentialsPrompt(SelectedInstance);
            UpdateCredentials();
        }

        private async Task<IEnumerable<Instance>> GetAllWindowsInstancesAsync()
        {
            IList<Instance> instances = await CurrentDataSource.GetInstanceListAsync();
            return instances.Where(x => x.IsRunning() && x.IsWindowsInstance()).OrderBy(x => x.Name);
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        protected internal override IList<string> RequiredApis => s_requiredApis;

        public override async void Publish()
        {
            IParsedProject project = PublishDialog.Project;

            try
            {
                ShellUtils.SaveAllFiles();

                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine(string.Format(Resources.GcePublishStepStartMessage, project.Name));

                PublishDialog.FinishFlow();

                string progressBarTitle = string.Format(Resources.GcePublishProgressMessage, SelectedInstance.Name);
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
                        SelectedInstance,
                        SelectedCredentials,
                        progress,
                        VsVersionUtils.ToolsPathProvider,
                        GcpOutputWindow.OutputLine);
                    deploymentDuration = DateTime.Now - startDeploymentTime;
                }

                if (result)
                {
                    GcpOutputWindow.OutputLine(string.Format(Resources.GcePublishSuccessMessage, project.Name, SelectedInstance.Name));
                    StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                    string url = SelectedInstance.GetDestinationAppUri();
                    GcpOutputWindow.OutputLine(string.Format(Resources.PublishUrlMessage, url));
                    if (OpenWebsite)
                    {
                        Process.Start(url);
                    }

                    EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Success, deploymentDuration));

                    if (LaunchRemoteDebugger)
                    {
                        AttachDebuggerDialog.AttachDebuggerWindow.PromptUser(SelectedInstance);
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

                EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        protected override void ClearLoadedProjectData()
        {
            Instances = Enumerable.Empty<Instance>();
        }

        protected override Task LoadProjectDataAlwaysAsync() => Task.Delay(0);

        protected override async Task LoadProjectDataIfValidAsync()
        {
            Instances = await GetAllWindowsInstancesAsync();
        }

        protected override void RefreshCanPublish()
        {
            CanPublish = IsValidGCPProject && !HasErrors && SelectedCredentials != null;
        }

        public override IPublishDialogStep Next()
        {
            throw new InvalidOperationException();
        }

        #endregion

        internal static GceStepViewModel CreateStep(
            IGceDataSource dataSource = null,
            IApiManager apiManager = null,
            Func<Google.Apis.CloudResourceManager.v1.Data.Project> pickProjectPrompt = null,
            IWindowsCredentialsStore currentWindowsCredentialStore = null,
            Action<Instance> manageCredentialsPrompt = null)
        {
            var content = new GceStepContent();
            var viewModel = new GceStepViewModel(
                content, dataSource, apiManager, pickProjectPrompt,
                currentWindowsCredentialStore, manageCredentialsPrompt);
            content.DataContext = viewModel;

            return viewModel;
        }

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
