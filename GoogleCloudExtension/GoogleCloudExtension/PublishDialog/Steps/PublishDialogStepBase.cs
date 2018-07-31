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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.PickProjectDialog;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog.Steps
{
    /// <summary>
    /// This is the base class for all step implementation, providing default implementations
    /// for the <seealso cref="IPublishDialogStep"/> interface.
    /// </summary>
    public abstract class PublishDialogStepBase : ValidatingViewModelBase, IPublishDialogStep
    {
        public const string ConfigurationPropertyName = "GoogleCloudPublishConfiguration";
        public const string DefaultConfiguration = "Release";
        private readonly IApiManager _apiManager;
        private bool _needsApiEnabled = false;
        private bool _isValidGcpProject = false;
        private AsyncProperty _loadProjectTask;
        private IEnumerable<string> _configurations;
        private string _selectedConfiguration;
        private string _lastConfiguration;

        protected internal IPublishDialog PublishDialog { get; }

        /// <summary>
        /// Indicates whether the publish button is active.
        /// </summary>
        public bool CanPublish
        {
            get => PublishCommand.CanExecuteCommand;
            protected set => PublishCommand.CanExecuteCommand = value;
        }

        /// <summary>
        /// The ID of the current Google Cloud Project.
        /// </summary>
        public string GcpProjectId => CredentialsStore.Default.CurrentProjectId;

        /// <summary>
        /// The command used to select the Google Cloud Project.
        /// </summary>
        public ProtectedCommand SelectProjectCommand { get; }

        /// <summary>
        /// Whether the GCP project selected needs APIs to be enabled before a deployment can be made.
        /// </summary>
        public bool NeedsApiEnabled
        {
            get { return _needsApiEnabled; }
            protected set
            {
                SetValueAndRaise(ref _needsApiEnabled, value);
                EnableApiCommand.CanExecuteCommand = value;
                RaisePropertyChanged(nameof(ShowInputControls));
            }
        }

        /// <summary>
        /// Tracks the load project task.
        /// </summary>
        public AsyncProperty LoadProjectTask
        {
            get => _loadProjectTask;
            set
            {
                LoadProjectTask.PropertyChanged -= LoadProjectTaskOnPropertyChanged;
                SetValueAndRaise(ref _loadProjectTask, value);
                LoadProjectTask.PropertyChanged += LoadProjectTaskOnPropertyChanged;
                LoadProjectTaskOnPropertyChanged(LoadProjectTask, new PropertyChangedEventArgs(null));
            }
        }

        /// <summary>
        /// Whether the input controls should be visible at this point.
        /// </summary>
        public virtual bool ShowInputControls =>
            !string.IsNullOrWhiteSpace(GcpProjectId)
            && LoadProjectTask.IsSuccess
            && !NeedsApiEnabled;

        /// <summary>
        /// The list of available build configurations.
        /// </summary>
        public IEnumerable<string> Configurations
        {
            get => _configurations;
            set
            {
                SelectedConfiguration = value?.FirstOrDefault(c => c == SelectedConfiguration) ??
                    value?.FirstOrDefault(c => c == _lastConfiguration) ??
                    value?.FirstOrDefault(c => c == DefaultConfiguration) ??
                    value?.FirstOrDefault();
                SetValueAndRaise(ref _configurations, value);
            }
        }

        /// <summary>
        /// Returns the <seealso cref="IApiManager"/> instance to use.
        /// </summary>
        private IApiManager CurrentApiManager => _apiManager ?? ApiManager.Default;

        /// <summary>
        /// The command to execute to enable the necessary APIs for the project.
        /// </summary>
        public ProtectedAsyncCommand EnableApiCommand { get; }

        /// <summary>
        /// List of APIs required for publishing to the current project.
        /// </summary>
        protected abstract IList<string> RequiredApis { get; }

        protected bool IsValidGcpProject
        {
            get { return _isValidGcpProject; }
            set
            {
                if (value != _isValidGcpProject)
                {
                    _isValidGcpProject = value;
                    RefreshCanPublish();
                    OnIsValidGcpProjectChanged();
                }
            }
        }

        public IProtectedCommand PublishCommand => PublishCommandAsync;

        protected internal abstract ProtectedAsyncCommand PublishCommandAsync { get; }

        /// <summary>
        /// The build configuration to publish.
        /// </summary>
        public string SelectedConfiguration
        {
            get => _selectedConfiguration;
            set => SetValueAndRaise(ref _selectedConfiguration, value);
        }

        private IUserPromptService UserPromptService => GoogleCloudExtensionPackage.Instance.UserPromptService;

        protected PublishDialogStepBase(
            IPublishDialog publishDialog)
        {
            _apiManager = GoogleCloudExtensionPackage.Instance.GetMefService<IApiManager>();
            PublishDialog = publishDialog;
            _loadProjectTask = new AsyncProperty(null);

            SelectProjectCommand = new ProtectedCommand(OnSelectProjectCommand, false);
            EnableApiCommand = new ProtectedAsyncCommand(OnEnableApiCommandAsync, false);
        }

        private void LoadProjectTaskOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshCanPublish();
            RaisePropertyChanged(nameof(ShowInputControls));
        }

        /// <summary>
        /// This method is called when IsValidGcpProject is set to a new value.
        /// </summary>
        protected virtual void OnIsValidGcpProjectChanged() { }

        /// <summary>
        /// Called every time this step moves on to the top of the navigation stack.
        /// </summary>
        /// <remarks>
        /// This method adds event handlers for <see cref="IPublishDialog.FlowFinished"/> and <see cref="ICredentialsStore.CurrentProjectIdChanged"/>,
        /// and Loads properties from the project file.
        /// </remarks>
        public void OnVisible()
        {
            AddHandlers();
            LoadProjectPropertiesBase();
            LoadProjectProperties();
            OnProjectChanged();
            SelectProjectCommand.CanExecuteCommand = true;
        }

        private void LoadProjectPropertiesBase()
        {
            _lastConfiguration = PublishDialog.Project.GetUserProperty(ConfigurationPropertyName);

            var rowNames = (IEnumerable)PublishDialog.Project.Project.ConfigurationManager.ConfigurationRowNames;
            // ConfigurationRowNames might be object[], which is not IEnumerable<string>.
            Configurations = rowNames.OfType<string>();
        }

        /// <summary>
        /// Called every time this step moves off the top of the navigation stack.
        /// </summary>
        /// <remarks>
        /// This method removes event handlers added by <see cref="OnVisible"/> and saves project properties.
        /// </remarks>
        public void OnNotVisible()
        {
            SaveProjectPropertiesBase();
            SaveProjectProperties();
            RemoveHandlers();
        }

        private void SaveProjectPropertiesBase()
        {
            PublishDialog.Project.SaveUserProperty(ConfigurationPropertyName, SelectedConfiguration);
        }

        /// <summary>
        /// Called whenever the current GCP Project changes, either from
        /// within this step or from somewhere else.
        /// </summary>
        private void OnProjectChanged()
        {
            RaisePropertyChanged(nameof(GcpProjectId));
            LoadProject();
        }

        /// <summary>
        /// Starts a new load project task, tracked by <see cref="LoadProjectTask"/>.
        /// </summary>
        public void LoadProject()
        {
            LoadProjectTask = new AsyncProperty(LoadProjectTaskAsync());
            PublishDialog.TrackTask(LoadProjectTask.ActualTask);

            async Task LoadProjectTaskAsync()
            {
                IsValidGcpProject = false;
                ClearLoadedProjectData();
                Task loadDataAlwaysTask = LoadAnyProjectDataAsync();

                await ValidateProjectAsync();

                if (IsValidGcpProject)
                {
                    await LoadValidProjectDataAsync();
                }

                await loadDataAlwaysTask;
            }
        }

        /// <summary>
        /// Checks to see if the given project is valid, and has required APIs enabled.
        /// Once complete, this task will have set <see cref="IsValidGcpProject"/> and <see cref="NeedsApiEnabled"/>.
        /// </summary>
        protected virtual async Task ValidateProjectAsync()
        {
            // Reset UI State
            IsValidGcpProject = false;
            NeedsApiEnabled = false;

            IList<string> requiredApis = RequiredApis;

            if (string.IsNullOrEmpty(GcpProjectId))
            {
                Debug.WriteLine("No project selected.");
            }
            else if (requiredApis.Count > 0
                && !await CurrentApiManager.AreServicesEnabledAsync(requiredApis))
            {
                Debug.WriteLine("APIs not enabled.");
                NeedsApiEnabled = true;
            }
            else
            {
                IsValidGcpProject = true;
            }
        }

        /// <summary>
        /// Called before loading a project to remove any data from a previous one.
        /// </summary>
        protected abstract void ClearLoadedProjectData();

        /// <summary>
        /// Called when loading a project. This will load data that needs to be loaded
        /// even if the project is not valid.
        /// </summary>
        protected abstract Task LoadAnyProjectDataAsync();

        /// <summary>
        /// Called when loading a project. This will load data that needs to be loaded
        /// only when the project is valid.
        /// </summary>
        protected abstract Task LoadValidProjectDataAsync();

        /// <summary>
        /// Callback function for when HasErrors may have changed.
        /// </summary>
        protected override void HasErrorsChanged()
        {
            base.HasErrorsChanged();
            RefreshCanPublish();
        }

        /// <summary>
        /// Updates <see cref="CanPublish"/> from the step properties.
        /// </summary>
        protected virtual void RefreshCanPublish()
        {
            CanPublish = IsValidGcpProject
                && LoadProjectTask.IsSuccess
                && !HasErrors;
        }

        private async Task OnEnableApiCommandAsync()
        {
            await CurrentApiManager.EnableServicesAsync(RequiredApis);
            LoadProject();
        }

        /// <summary>
        /// Called when the flow this dialog was part of finishes.
        /// Mostly calls when publication either happens or the dialog is closed.
        /// Used to perform cleanup actions and restore the initial state.
        /// </summary>
        protected internal virtual void OnFlowFinished()
        {
            RemoveHandlers();
            SaveProjectProperties();
            IsValidGcpProject = false;
            LoadProjectTask = new AsyncProperty(null);
            NeedsApiEnabled = false;
            SelectProjectCommand.CanExecuteCommand = false;

            ClearLoadedProjectData();
        }

        private void OnSelectProjectCommand()
        {
            Project selectedProject = UserPromptService.PromptUser(
                new PickProjectIdWindowContent(Resources.PublishDialogPickProjectHelpMessage, true));
            bool hasChanged = !string.Equals(selectedProject?.ProjectId, CredentialsStore.Default.CurrentProjectId);
            if (selectedProject?.ProjectId != null && hasChanged)
            {
                CredentialsStore.Default.UpdateCurrentProject(selectedProject);
            }
            else if (!hasChanged)
            {
                LoadProject();
            }
        }

        private void OnProjectChanged(object sender, EventArgs e) => OnProjectChanged();

        private void OnFlowFinished(object sender, EventArgs e) => OnFlowFinished();

        private void AddHandlers()
        {
            PublishDialog.FlowFinished += OnFlowFinished;
            CredentialsStore.Default.CurrentProjectIdChanged += OnProjectChanged;
        }

        private void RemoveHandlers()
        {
            PublishDialog.FlowFinished -= OnFlowFinished;
            CredentialsStore.Default.CurrentProjectIdChanged -= OnProjectChanged;
        }

        /// <summary>
        /// Loads the step specific properties from the project file.
        /// </summary>
        protected abstract void LoadProjectProperties();

        /// <summary>
        /// Saves the step specific properties to the project file.
        /// </summary>
        protected abstract void SaveProjectProperties();
    }
}
