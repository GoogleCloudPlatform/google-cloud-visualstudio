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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
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

namespace GoogleCloudExtension.PublishDialogSteps.GceStep
{
    /// <summary>
    /// The view model for the publish step that publishes the project to a GCE VM.
    /// </summary>
    public class GceStepViewModel : PublishDialogStepBase
    {
        private readonly GceStepContent _content;
        private IPublishDialog _publishDialog;
        private Instance _selectedInstance;
        private IEnumerable<WindowsInstanceCredentials> _credentials;
        private WindowsInstanceCredentials _selectedCredentials;
        private bool _openWebsite = true;
        private bool _launchRemoteDebugger;

        /// <summary>
        /// The asynchrnous value that will resolve to the list of instances in the current GCP Project, and that are
        /// the available target for the publish process.
        /// </summary>
        public AsyncProperty<IEnumerable<Instance>> Instances { get; }

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
            private set { SetValueAndRaise(ref _credentials, value); }
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
                RaisePropertyChanged(nameof(HasSelectedCredentials));
                CanPublish = value != null;
            }
        }

        /// <summary>
        /// Returns whether there are credentials selected for the publish process.
        /// </summary>
        public bool HasSelectedCredentials => SelectedCredentials != null;

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

        private GceStepViewModel(GceStepContent content)
        {
            _content = content;

            Instances = AsyncPropertyUtils.CreateAsyncProperty(GetAllWindowsInstances());

            ManageCredentialsCommand = new ProtectedCommand(OnManageCredentialsCommand, canExecuteCommand: false);
        }

        private void OnManageCredentialsCommand()
        {
            ManageWindowsCredentialsWindow.PromptUser(SelectedInstance);
            UpdateCredentials();
        }

        private async Task<IEnumerable<Instance>> GetAllWindowsInstances()
        {
            var dataSource = new GceDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var instances = await dataSource.GetInstanceListAsync();
            return instances.Where(x => x.IsRunning() && x.IsWindowsInstance()).OrderBy(x => x.Name);
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        public override async void Publish()
        {
            var project = _publishDialog.Project;

            try
            {
                ShellUtils.SaveAllFiles();

                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine(String.Format(Resources.GcePublishStepStartMessage, project.Name));

                _publishDialog.FinishFlow();

                TimeSpan deploymentDuration;
                bool result;
                using (var frozen = StatusbarHelper.Freeze())
                using (var animationShown = StatusbarHelper.ShowDeployAnimation())
                using (var progress = StatusbarHelper.ShowProgressBar(String.Format(Resources.GcePublishProgressMessage, SelectedInstance.Name)))
                using (var deployingOperation = ShellUtils.SetShellUIBusy())
                {
                    var startDeploymentTime = DateTime.Now;
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
                    GcpOutputWindow.OutputLine(String.Format(Resources.GcePublishSuccessMessage, project.Name, SelectedInstance.Name));
                    StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                    var url = SelectedInstance.GetDestinationAppUri();
                    GcpOutputWindow.OutputLine(String.Format(Resources.PublishUrlMessage, url));
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
                    GcpOutputWindow.OutputLine(String.Format(Resources.GcePublishFailedMessage, project.Name));
                    StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Failure));
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.GcePublishFailedMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);

                EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;

            _publishDialog.TrackTask(Instances.ValueTask);
        }

        #endregion

        internal static GceStepViewModel CreateStep()
        {
            var content = new GceStepContent();
            var viewModel = new GceStepViewModel(content);
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
                Credentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(SelectedInstance);
            }
            SelectedCredentials = Credentials.FirstOrDefault();
        }
    }
}
