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
using Microsoft.VisualStudio.Threading;
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
        private bool _canGoNext = false;
        private bool _canPublish = false;
        private readonly IApiManager _apiManager;
        private bool _loadingProject = false;
        private bool _needsApiEnabled = false;
        private bool _generalError = false;
        private readonly Func<Project> _pickProjectPrompt;
        private bool _isValidGcpProject = false;
        private IList<string> _requiredApis = new List<string>();
        private Task _asyncAction = TplExtensions.CompletedTask;

        private Func<Project> PickProjectPrompt => _pickProjectPrompt ??
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
                {
                    return null;
                }
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
                RefreshCanPublish();
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
                RefreshCanPublish();
                RaisePropertyChanged(nameof(ShowInputControls));
            }
        }

        /// <summary>
        /// Whether the input controls should be visible at this point.
        /// </summary>
        public virtual bool ShowInputControls =>
            PublishDialog != null
            && !string.IsNullOrWhiteSpace(GcpProjectId)
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
        protected internal Task AsyncAction
        {
            get { return _asyncAction; }
            private set
            {
                if (value == null)
                {
                    throw new ArgumentNullException($"{nameof(AsyncAction)} can't be null.");
                }
                _asyncAction = value;
            }
        }

        /// <summary>
        /// Event raised whenever <seealso cref="CanGoNext"/> value changes.
        /// </summary>
        public event EventHandler CanGoNextChanged;

        /// <summary>
        /// Event raised whenever <seealso cref="CanPublish"/> value changes.
        /// </summary>
        public event EventHandler CanPublishChanged;

        /// <summary>
        /// The command to execute to enable the necessary APIs for the project.
        /// </summary>
        public ProtectedAsyncCommand EnableApiCommand { get; }

        /// <summary>
        /// List of APIs required for publishing to the current project.
        /// This property is just a wrapper on the abstract method <see cref="ApisRequieredForPublishing"/>
        /// so as to guarantee a non null result.
        /// </summary>
        private IList<string> RequiredApis => ApisRequieredForPublishing() ?? new List<string>();

        protected bool IsValidGcpProject
        {
            get { return _isValidGcpProject; }
            set
            {
                if (value != _isValidGcpProject)
                {
                    _isValidGcpProject = value;
                    RefreshCanPublish();
                }
            }
        }

        protected PublishDialogStepBase(IApiManager apiManager, Func<Project> pickProjectPrompt)
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

        /// <summary>
        /// Returns the next step to navigate to when the next button is pressed.
        /// </summary>
        /// <returns>The next step to navigate to.</returns>
        public abstract IPublishDialogStep Next();

        /// <summary>
        /// Performs the publish step action, will only be called if <seealso cref="CanPublish"/> return true.
        /// </summary>
        public abstract void Publish();

        /// <summary>
        /// Called every time that this step is at the top of the navigation stack and therefore visible.
        /// </summary>
        /// <param name="dialog">The dialog that is hosting this step.</param>
        public virtual async void OnVisible(IPublishDialog dialog)
        {
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
        /// </summary>
        protected async Task OnProjectChangedAsync()
        {
            // Start the task that reloads the project.
            Task reloadTask = ReloadProjectAsync();

            // While the project is loading signal property changed.
            // Basically telling the UI to refresh the texts.
            RaisePropertyChanged(nameof(GcpProjectId));

            // Wait for project loading to be done.
            await reloadTask;
        }

        protected async Task ReloadProjectAsync()
        {
            try
            {
                LoadingProject = true;
                GeneralError = false;

                ClearLoadedProjectData();
                Task loadDataAlwaysTask = LoadAnyProjectDataAsync();

                await ValidateProjectAsync();

                if (IsValidGcpProject)
                {
                    await LoadValidProjectDataAsync();
                }

                await loadDataAlwaysTask;
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                IsValidGcpProject = false;
                CanGoNext = false;
                GeneralError = true;
            }
            finally
            {
                LoadingProject = false;
            }
        }

        protected virtual async Task ValidateProjectAsync()
        {
            // Reset UI State
            IsValidGcpProject = false;
            CanGoNext = false;
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
        /// Called during project validation.
        /// A project can only be valid for publishing it the required APIs
        /// are enabled.
        /// </summary>
        /// <returns>The list of required APIs or an empty list.</returns>
        protected internal abstract IList<string> ApisRequieredForPublishing();

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
                && !LoadingProject
                && !HasErrors
                && !GeneralError;
        }

        protected async Task OnEnableApiCommandAsync()
        {
            await CurrentApiManager.EnableServicesAsync(RequiredApis);
            await ReloadProjectAsync();
        }

        /// <summary>
        /// Called when the flow this dialog was part of finishes.
        /// Mostly calls when publication either happens or the dialog is closed.
        /// Used to perform cleanup actions and restore the initial state.
        /// </summary>
        protected internal virtual void OnFlowFinished()
        {
            RemoveHandlers();
            PublishDialog = null;
            CanGoNext = false;
            IsValidGcpProject = false;
            LoadingProject = false;
            GeneralError = false;
            NeedsApiEnabled = false;
            SelectProjectCommand.CanExecuteCommand = false;
            AsyncAction = TplExtensions.CompletedTask;

            ClearLoadedProjectData();
        }

        private async void OnSelectProjectCommand()
        {
            Project selectedProject = PickProjectPrompt();
            bool hasChanged = !string.Equals(selectedProject?.ProjectId, CredentialsStore.Default.CurrentProjectId);
            if (selectedProject?.ProjectId != null && hasChanged)
            {
                CredentialsStore.Default.UpdateCurrentProject(selectedProject);
            }
            else if (!hasChanged)
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
            // Checking for null in case it was never pushed in a dialog.
            if (PublishDialog != null)
            {
                PublishDialog.FlowFinished += OnFlowFinished;
                CredentialsStore.Default.CurrentProjectIdChanged += OnProjectChanged;
            }
        }

        private void RemoveHandlers()
        {
            // Checking for null in case it was never pushed in a dialog.
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
