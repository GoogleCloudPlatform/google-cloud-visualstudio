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
        private bool _loadingProject = false;
        private bool _needsApiEnabled = false;
        private bool _generalError = false;

        internal Func<Project> PickProjectPrompt =
            () => PickProjectIdWindow.PromptUser(Resources.PublishDialogPickProjectHelpMessage, allowAccountChange: true);

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
                RaisePropertyChanged(nameof(ShowInputControls));
            }
        }

        /// <summary>
        /// Whether the project is loaded, which include validating that the project is correctly
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
        public virtual bool ShowInputControls => !LoadingProject && !NeedsApiEnabled && !GeneralError;

        /// <summary>
        /// Returns the <seealso cref="IApiManager"/> instance to use.
        /// </summary>
        protected IApiManager CurrentApiManager => _apiManager ?? ApiManager.Default;

        /// <summary>
        /// The task that tracks the project loading process.
        /// </summary>
        internal Task LoadingProjectTask { get; set; }

        public event EventHandler CanGoNextChanged;

        public event EventHandler CanPublishChanged;

        protected PublishDialogStepBase()
            : this(null)
        { }

        /// <inheritdoc />
        protected PublishDialogStepBase(IApiManager apiManager)
        {
            _apiManager = apiManager;

            SelectProjectCommand = new ProtectedCommand(OnSelectProjectCommand);
            CredentialsStore.Default.CurrentProjectIdChanged += (sender, args) =>
            {
                RaisePropertyChanged(nameof(GcpProjectId));
            };
        }

        public virtual IPublishDialogStep Next()
        {
            throw new NotImplementedException();
        }

        public virtual void Publish()
        {
            throw new NotImplementedException();
        }
        public virtual void OnPushedToDialog(IPublishDialog dialog)
        {
            PublishDialog = dialog;
        }

        protected virtual void OnProjectChanged()
        { }

        private void OnSelectProjectCommand()
        {
            Project selectedProject = PickProjectPrompt();
            if (selectedProject?.ProjectId != null && selectedProject?.ProjectId != CredentialsStore.Default.CurrentProjectId)
            {
                CredentialsStore.Default.UpdateCurrentProject(selectedProject);
                OnProjectChanged();
            }
        }
    }
}
