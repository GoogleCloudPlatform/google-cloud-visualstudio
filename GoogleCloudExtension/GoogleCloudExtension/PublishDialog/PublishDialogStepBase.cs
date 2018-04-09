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
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// This is the base class for all step implementation, providing default implementations
    /// for the <seealso cref="IPublishDialogStep"/> interface.
    /// </summary>
    public abstract class PublishDialogStepBase : ValidatingViewModelBase, IPublishDialogStep
    {
        private bool _canGoNext;
        private bool _canPublish;
        private readonly IApiManager _apiManager;
        private readonly Func<Project> _pickProjectPrompt;
        private bool _loadingProject;
        private bool _needsApiEnabled;
        private bool _generalError;
        private bool _isValidGCPProject;

        protected Func<Project> PickProjectPrompt => _pickProjectPrompt ??
            (() => PickProjectIdWindow.PromptUser(Resources.PublishDialogPickProjectHelpMessage, allowAccountChange: true));

        protected internal IPublishDialog PublishDialog { get; private set; }

        /// <summary>
        /// Indicates whether the next button is active.
        /// </summary>
        public bool CanGoNext
        {
            get { return _canGoNext; }
            protected set
            {
                if (_canGoNext != value)
                {
                    _canGoNext = value;
                    CanGoNextChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Indicates whether the publish button is active.
        /// </summary>
        public bool CanPublish
        {
            get { return _canPublish; }
            protected set
            {
                if (_canPublish != value)
                {
                    _canPublish = value;
                    CanPublishChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// The content of the step.
        /// </summary>
        public abstract FrameworkElement Content { get; }

        /// <summary>
        /// The ID of the current Google Cloud Project.
        /// </summary>
        public string GcpProjectId
        {
            get
            {
                if (PublishDialog == null)
                    return null;
                return CredentialsStore.Default.CurrentProjectId;
            }
        }

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
        /// Whether the project is loading, which includes validating that the project is correctly
        /// setup for deployment and loading the necessary data to display to the user.
        /// </summary>
        public bool LoadingProject
        {
            get { return _loadingProject; }
            protected set
            {
                SetValueAndRaise(ref _loadingProject, value);
                RaisePropertyChanged(nameof(ShowInputControls));
            }
        }

        /// <summary>
        /// Whether there was an error validating the project.
        /// </summary>
        public bool GeneralError
        {
            get { return _generalError; }
            set
            {
                SetValueAndRaise(ref _generalError, value);
                RaisePropertyChanged(nameof(ShowInputControls));
            }
        }

        /// <summary>
        /// Whether the input controls should be visible at this point.
        /// </summary>
        public virtual bool ShowInputControls =>
            PublishDialog != null
            && !LoadingProject
            && !NeedsApiEnabled
            && !GeneralError;

        /// <summary>
        /// Returns the <seealso cref="IApiManager"/> instance to use.
        /// </summary>
        protected IApiManager CurrentApiManager => _apiManager ?? ApiManager.Default;

        /// <summary>
        /// The task that tracks the asycn actions performed from the Dialog.
        /// </summary>
        protected internal Task AsyncAction { get; set; }

        /// <inheritdoc />
        public event EventHandler CanGoNextChanged;

        /// <inheritdoc />
        public event EventHandler CanPublishChanged;

        /// <summary>
        /// The command to execute to enable the necessary APIs for the project.
        /// </summary>
        public ProtectedAsyncCommand EnableApiCommand { get; }

        protected internal abstract IList<string> RequiredApis { get; }

        protected bool IsValidGCPProject
        {
            get { return _isValidGCPProject; }
            set
            {
                _isValidGCPProject = value;
                RefreshCanPublish();
            }
        }

        protected PublishDialogStepBase()
            : this(null, null)
        { }

        protected internal PublishDialogStepBase(IApiManager apiManager, Func<Project> pickProjectPrompt)
        {
            _apiManager = apiManager;
            _pickProjectPrompt = pickProjectPrompt;

            SelectProjectCommand = new ProtectedCommand(OnSelectProjectCommand, false);
            EnableApiCommand = new ProtectedAsyncCommand(async () => 
            {
                StartAndTrack(OnEnableApiCommandAsync);
                await AsyncAction;
            }, false);
        }

        /// <inheritdoc />
        public abstract IPublishDialogStep Next();

        /// <inheritdoc />
        public abstract void Publish();

        /// <inheritdoc />
        public virtual async void OnVisible(IPublishDialog dialog)
        {
            ///Impossible right now but possible in the future?,
            ///this is in case this step was being shown in another dialog.
            ///The class API allows it, so better do something about it here.
            RemoveHandlers();
            PublishDialog = dialog;

            StartAndTrack(InitializeDialogAsync);

            AddHandlers();
            SelectProjectCommand.CanExecuteCommand = true;

            await AsyncAction;
        }

        protected virtual async Task InitializeDialogAsync()
        {
            await OnProjectChangedAsync();
        }

        /// <summary>
        /// Called whenever the current GCP Project changes, either from
        /// within this step or from somewhere else.
        /// Will be probably overwritten by children to refresh project
        /// dependent state.
        /// </summary>
        protected virtual async Task OnProjectChangedAsync()
        {
            await ReloadProjectAsync();

            RaisePropertyChanged(nameof(GcpProjectId));
        }        

        protected virtual async Task ReloadProjectAsync()
        {
            try
            {
                LoadingProject = true;
                GeneralError = false;
                Task loadDataAlwaysTask = LoadProjectDataAlwaysAsync();

                if (await ValidateProjectAsync())
                {
                    await LoadProjectDataIfValidAsync();
                }

                await loadDataAlwaysTask;
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                IsValidGCPProject = false;
                CanGoNext = false;
                GeneralError = true;
            }
            finally
            {
                LoadingProject = false;
            }
        }

        protected virtual async Task<bool> ValidateProjectAsync()
        {
            // Reset UI State
            IsValidGCPProject = false;
            CanGoNext = false;
            NeedsApiEnabled = false;

            if (string.IsNullOrEmpty(GcpProjectId))
            {
                Debug.WriteLine("No project selected.");
                return false;
            }
            if (RequiredApis?.Count > 0
                && !await CurrentApiManager.AreServicesEnabledAsync(RequiredApis))
            {
                Debug.WriteLine("APIs not enabled.");
                NeedsApiEnabled = true;
                return false;
            }

            return true;
        }

        protected abstract Task LoadProjectDataAlwaysAsync();

        protected abstract Task LoadProjectDataIfValidAsync();

        protected override void HasErrorsChanged()
        {
            base.HasErrorsChanged();
            RefreshCanPublish();
        }

        private void RefreshCanPublish()
        {
            CanPublish = IsValidGCPProject && !HasErrors;
        }

        protected virtual async Task OnEnableApiCommandAsync()
        {
            await CurrentApiManager.EnableServicesAsync(RequiredApis);
            await ReloadProjectAsync();
        }

        /// <inheritdoc />
        protected internal virtual void OnFlowFinished()
        {
            RemoveHandlers();
            PublishDialog = null;
            CanGoNext = false;
            IsValidGCPProject = false;
            LoadingProject = false;
            GeneralError = false;
            NeedsApiEnabled = false;
            SelectProjectCommand.CanExecuteCommand = false;
        }

        private async void OnSelectProjectCommand()
        {
            Project selectedProject = PickProjectPrompt();
            bool hasChanged = !string.Equals(selectedProject?.ProjectId, CredentialsStore.Default.CurrentProjectId);
            if (selectedProject?.ProjectId != null && hasChanged)
            {
                CredentialsStore.Default.UpdateCurrentProject(selectedProject);
            }
            else if(!hasChanged)
            {
                StartAndTrack(ReloadProjectAsync);
                await AsyncAction;
            }
        }

        private async void OnProjectChanged(object sender, EventArgs e)
        {
            StartAndTrack(OnProjectChangedAsync);
            await AsyncAction;
        }

        private void OnFlowFinished(object sender, EventArgs e)
            => OnFlowFinished();

        private void AddHandlers()
        {
            ///Checking for null in case it was never pushed in a dialog.
            if (PublishDialog != null)
            {
                PublishDialog.FlowFinished += OnFlowFinished;
                CredentialsStore.Default.CurrentProjectIdChanged += OnProjectChanged;
            }
        }

        private void RemoveHandlers()
        {
            ///Checking for null in case it was never pushed in a dialog.
            if (PublishDialog != null)
            {
                PublishDialog.FlowFinished -= OnFlowFinished;
                CredentialsStore.Default.CurrentProjectIdChanged -= OnProjectChanged;
            }
        }

        protected void StartAndTrack(Func<Task> asyncAction)
        {            
            AsyncAction = asyncAction();
            PublishDialog.TrackTask(AsyncAction);            
        }
    }
}
