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
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog.Steps
{
    /// <summary>
    /// This is the base class for all step implementation, providing default implementations
    /// for the <seealso cref="IPublishDialogStep"/> interface.
    /// </summary>
    public abstract class PublishDialogStepBase : ValidatingViewModelBase, IPublishDialogStep
    {
        private readonly IApiManager _apiManager;
        private bool _needsApiEnabled = false;
        private readonly Func<Project> _pickProjectPrompt;
        private bool _isValidGcpProject = false;
        private AsyncProperty _loadProjectTask;

        private Func<Project> PickProjectPrompt => _pickProjectPrompt ??
            (() => PickProjectIdWindow.PromptUser(Resources.PublishDialogPickProjectHelpMessage, allowAccountChange: true));

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
        /// Returns the <seealso cref="IApiManager"/> instance to use.
        /// </summary>
        private IApiManager CurrentApiManager => _apiManager ?? ApiManager.Default;

        /// <summary>
        /// The command to execute to enable the necessary APIs for the project.
        /// </summary>
        public ProtectedAsyncCommand EnableApiCommand { get; }

        /// <summary>
        /// List of APIs required for publishing to the current project.
        /// This property is just a wrapper on the abstract method <see cref="ApisRequieredForPublishing"/>
        /// so as to guarantee a non null result.
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

        public abstract IProtectedCommand PublishCommand { get; }

        protected PublishDialogStepBase(
            IApiManager apiManager,
            Func<Project> pickProjectPrompt,
            IPublishDialog publishDialog)
        {
            _apiManager = apiManager;
            _pickProjectPrompt = pickProjectPrompt;
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
        /// Called every time that this step is at the top of the navigation stack and therefore visible.
        /// </summary>
        public virtual async Task OnVisibleAsync()
        {

            await InitializeDialogAsync();

            AddHandlers();
            SelectProjectCommand.CanExecuteCommand = true;
        }

        public void OnNotVisible()
        {
            RemoveHandlers();
        }

        protected virtual async Task InitializeDialogAsync()
        {
            await OnProjectChangedAsync();
        }

        /// <summary>
        /// Called whenever the current GCP Project changes, either from
        /// within this step or from somewhere else.
        /// </summary>
        private async Task OnProjectChangedAsync()
        {
            RaisePropertyChanged(nameof(GcpProjectId));
            await LoadProjectAsync();
        }

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

        public Task LoadProjectAsync()
        {
            LoadProject();
            return LoadProjectTask.SafeTask;
        }

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

        protected override void HasErrorsChanged()
        {
            base.HasErrorsChanged();
            RefreshCanPublish();
        }

        /// <summary>
        /// This is the base class. By default steps cannot publish.
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
            await LoadProjectAsync();
        }

        /// <summary>
        /// Called when the flow this dialog was part of finishes.
        /// Mostly calls when publication either happens or the dialog is closed.
        /// Used to perform cleanup actions and restore the initial state.
        /// </summary>
        protected internal virtual void OnFlowFinished()
        {
            RemoveHandlers();
            IsValidGcpProject = false;
            LoadProjectTask = new AsyncProperty(null);
            NeedsApiEnabled = false;
            SelectProjectCommand.CanExecuteCommand = false;

            ClearLoadedProjectData();
        }

        private void OnSelectProjectCommand()
        {
            Project selectedProject = PickProjectPrompt();
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

        private async void OnProjectChanged(object sender, EventArgs e)
        {
            await OnProjectChangedAsync();
        }

        private void OnFlowFinished(object sender, EventArgs e)
            => OnFlowFinished();

        private void AddHandlers()
        {
            // Checking for null in case it was never pushed in a dialog.
            if (PublishDialog != null)
            {
                PublishDialog.FlowFinished += OnFlowFinished;
                CredentialsStore.Default.CurrentProjectIdChanged += OnProjectChanged;
            }
        }

        private void RemoveHandlers()
        {
            PublishDialog.FlowFinished -= OnFlowFinished;
            CredentialsStore.Default.CurrentProjectIdChanged -= OnProjectChanged;
        }
    }
}
