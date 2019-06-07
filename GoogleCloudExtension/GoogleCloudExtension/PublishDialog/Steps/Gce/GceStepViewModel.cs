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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.AttachDebuggerDialog;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.PublishDialog.Steps.Gce
{
    /// <summary>
    /// The view model for the publish step that publishes the project to a GCE VM.
    /// </summary>
    public class GceStepViewModel : PublishDialogStepBase
    {
        public const string InstanceNameProjectPropertyName = "GoogleComputeEnginePublishInstanceName";
        public const string InstanceZoneProjectPropertyName = "GoogleComputeEnginePublishInstanceZone";
        public const string SiteNameProjectPropertyName = "GoogleComputeEnginePublishSiteName";
        public const string InstanceUserNameProjectPropertyName = "GoogleComputeEnginePublishInstanceUserName";
        public const string OpenWebsiteProjectPropertyName = "GoogleComputeEnginePublishOpenWebsite";
        public const string LaunchRemoteDebuggerProjectPropertyName = "GoogleComputeEnginePublishLaunchRemoteDebugger";
        public const string DefaultSiteName = "Default Web Site";

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
        private string _siteName = DefaultSiteName;
        private IEnumerable<WindowsInstanceCredentials> _credentials = Enumerable.Empty<WindowsInstanceCredentials>();
        private WindowsInstanceCredentials _selectedCredentials = null;
        private bool _openWebsite = true;
        private bool _launchRemoteDebugger = false;
        private IEnumerable<Instance> _instances = Enumerable.Empty<Instance>();
        private string _lastInstanceNameProperty;
        private string _lastInstanceZoneNameProperty;
        private string _lastInstanceUserNameProperty;

        private readonly Lazy<IWindowsVmDeployment> _deploymentService =
            GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IWindowsVmDeployment>();

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
            get => _instances;
            private set
            {
                SelectedInstance = value?.FirstOrDefault(
                        i => i.Name == SelectedInstance?.Name && i.GetZoneName() == SelectedInstance?.GetZoneName()) ??
                    value?.FirstOrDefault(
                        i => i.Name == _lastInstanceNameProperty && i.GetZoneName() == _lastInstanceZoneNameProperty) ??
                    value?.FirstOrDefault();
                SetValueAndRaise(ref _instances, value);
            }
        }

        /// <summary>
        /// The selected GCE VM that will be the target of the publish process.
        /// </summary>
        public Instance SelectedInstance
        {
            get => _selectedInstance;
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
            get => _credentials;
            private set
            {
                SelectedCredentials = value?.FirstOrDefault(c => c.User == SelectedCredentials?.User) ??
                    value?.FirstOrDefault(c => c.User == _lastInstanceUserNameProperty) ?? value?.FirstOrDefault();
                SetValueAndRaise(ref _credentials, value);
            }
        }

        /// <summary>
        /// The selected <seealso cref="WindowsInstanceCredentials"/> to use for the publish process.
        /// </summary>
        public WindowsInstanceCredentials SelectedCredentials
        {
            get => _selectedCredentials;
            set
            {
                SetValueAndRaise(ref _selectedCredentials, value);
                RefreshCanPublish();
            }
        }

        /// <summary>
        /// The name of the website on IIS to target.
        /// </summary>
        public string SiteName
        {
            get => _siteName;
            set => SetValueAndRaise(ref _siteName, value);
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
            get => _openWebsite;
            set => SetValueAndRaise(ref _openWebsite, value);
        }

        /// <summary>
        /// Whether to attach debugger after publising.
        /// </summary>
        public bool LaunchRemoteDebugger
        {
            get => _launchRemoteDebugger;
            set => SetValueAndRaise(ref _launchRemoteDebugger, value);
        }

        public override string Title { get; }
        protected internal override ProtectedAsyncCommand PublishCommandAsync { get; }

        private IGceDataSource CurrentDataSource =>
            _dataSource ?? new GceDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.Instance.ApplicationName);

        private IWindowsCredentialsStore CurrentWindowsCredentialsStore =>
            _currentWindowsCredentialStore ?? WindowsCredentialsStore.Default;

        private Action<Instance> ManageCredentialsPrompt =>
            _manageCredentialsPrompt ?? ManageWindowsCredentialsWindow.PromptUser;

        public GceStepViewModel(
            IGceDataSource dataSource,
            IWindowsCredentialsStore currentWindowsCredentialStore,
            Action<Instance> manageCredentialsPrompt,
            IPublishDialog publishDialog)
            : base(publishDialog)
        {
            _dataSource = dataSource;
            _currentWindowsCredentialStore = currentWindowsCredentialStore;
            _manageCredentialsPrompt = manageCredentialsPrompt;

            ManageCredentialsCommand = new ProtectedCommand(OnManageCredentialsCommand, false);
            RefreshInstancesCommand = new ProtectedCommand(
                () => PublishDialog.TrackTask(LoadValidProjectDataAsync()), false);
            PublishCommandAsync = new ProtectedAsyncCommand(PublishAsync);

            Title = string.Format(Resources.GcePublishStepTitle, publishDialog.Project.Name);
        }

        protected override void OnIsValidGcpProjectChanged() => RefreshInstancesCommand.CanExecuteCommand = IsValidGcpProject;

        private void OnManageCredentialsCommand()
        {
            ManageCredentialsPrompt(SelectedInstance);
            UpdateCredentials();
        }

        #region IPublishDialogStep

        private async Task PublishAsync()
        {
            IParsedDteProject project = PublishDialog.Project;
            Instance selectedInstance = SelectedInstance;
            string targetDeployPath = SiteName;
            WindowsInstanceCredentials selectedCredentials = SelectedCredentials;

            PublishDialog.FinishFlow();

            try
            {
                DateTime startDeploymentTime = DateTime.Now;
                bool result = await _deploymentService.Value.PublishProjectAsync(
                    project,
                    selectedInstance,
                    selectedCredentials,
                    targetDeployPath,
                    SelectedConfiguration);
                TimeSpan deploymentDuration = DateTime.Now - startDeploymentTime;

                if (result)
                {
                    await GcpOutputWindow.Default.OutputLineAsync(
                        string.Format(Resources.GcePublishSuccessMessage, project.Name, selectedInstance.Name));
                    await StatusbarHelper.Default.SetTextAsync(Resources.PublishSuccessStatusMessage);

                    string url = selectedInstance.GetDestinationAppUri();
                    await GcpOutputWindow.Default.OutputLineAsync(string.Format(Resources.PublishUrlMessage, url));
                    if (OpenWebsite)
                    {
                        Process.Start(url);
                    }

                    EventsReporterWrapper.ReportEvent(
                        GceDeployedEvent.Create(CommandStatus.Success, deploymentDuration));

                    if (LaunchRemoteDebugger)
                    {
                        AttachDebuggerWindow.PromptUser(selectedInstance);
                    }
                }
                else
                {
                    await GcpOutputWindow.Default.OutputLineAsync(string.Format(Resources.GcePublishFailedMessage, project.Name));
                    await StatusbarHelper.Default.SetTextAsync(Resources.PublishFailureStatusMessage);

                    EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Failure));
                }
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                await GcpOutputWindow.Default.OutputLineAsync(string.Format(Resources.GcePublishFailedMessage, project.Name));
                await StatusbarHelper.Default.SetTextAsync(Resources.PublishFailureStatusMessage);

                EventsReporterWrapper.ReportEvent(GceDeployedEvent.Create(CommandStatus.Failure));
            }
        }

        /// <summary>
        /// Clearing instances from a potential previous project.
        /// </summary>
        protected override void ClearLoadedProjectData() => Instances = Enumerable.Empty<Instance>();

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

        protected override void LoadProjectProperties()
        {
            _lastInstanceNameProperty = PublishDialog.Project.GetUserProperty(InstanceNameProjectPropertyName);
            _lastInstanceZoneNameProperty = PublishDialog.Project.GetUserProperty(InstanceZoneProjectPropertyName);
            _lastInstanceUserNameProperty =
                PublishDialog.Project.GetUserProperty(InstanceUserNameProjectPropertyName);
            string openWebsiteProperty = PublishDialog.Project.GetUserProperty(OpenWebsiteProjectPropertyName);
            if (bool.TryParse(openWebsiteProperty, out bool openWebsite))
            {
                OpenWebsite = openWebsite;
            }

            string launchRemoteDebuggerProperty =
                PublishDialog.Project.GetUserProperty(LaunchRemoteDebuggerProjectPropertyName);
            if (bool.TryParse(launchRemoteDebuggerProperty, out bool launchRemoteDebugger))
            {
                LaunchRemoteDebugger = launchRemoteDebugger;
            }

            string siteNamePropertyValue = PublishDialog.Project.GetUserProperty(SiteNameProjectPropertyName);
            SiteName = string.IsNullOrWhiteSpace(siteNamePropertyValue) ? DefaultSiteName : siteNamePropertyValue;
        }

        protected override void SaveProjectProperties()
        {
            PublishDialog.Project.SaveUserProperty(InstanceNameProjectPropertyName, SelectedInstance?.Name);
            PublishDialog.Project.SaveUserProperty(InstanceZoneProjectPropertyName, SelectedInstance?.GetZoneName());
            PublishDialog.Project.SaveUserProperty(InstanceUserNameProjectPropertyName, SelectedCredentials?.User);
            PublishDialog.Project.SaveUserProperty(OpenWebsiteProjectPropertyName, OpenWebsite.ToString());
            PublishDialog.Project.SaveUserProperty(
                LaunchRemoteDebuggerProjectPropertyName,
                LaunchRemoteDebugger.ToString());
            PublishDialog.Project.SaveUserProperty(SiteNameProjectPropertyName, SiteName);
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
