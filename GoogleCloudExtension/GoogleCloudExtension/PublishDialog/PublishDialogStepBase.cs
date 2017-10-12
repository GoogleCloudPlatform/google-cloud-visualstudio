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
using GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
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
        internal Func<string, Project> PickProjectPrompt = PickProjectIdWindow.PromptUser;
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

        public event EventHandler CanGoNextChanged;

        public event EventHandler CanPublishChanged;

        /// <inheritdoc />
        protected PublishDialogStepBase()
        {
            SelectProjectCommand = new ProtectedCommand(OnSelectProjectCommand);
            CredentialsStore.Default.CurrentProjectIdChanged += (sender, args) =>
            {
                RaisePropertyChanged(nameof(GcpProjectId));
            };
        }

        public virtual IPublishDialogStep Next() => throw new NotImplementedException();

        public virtual void Publish() => throw new NotImplementedException();

        public virtual void OnPushedToDialog(IPublishDialog dialog)
        {
            PublishDialog = dialog;
        }

        private void OnSelectProjectCommand()
        {
            string pickProjectDialogTitle = string.Format(
                Resources.PublishDialogSelectGcpProjectTitle, PublishDialog.Project.Name);
            Project selectedProject = PickProjectPrompt(pickProjectDialogTitle);
            if (selectedProject?.ProjectId != null)
            {
                CredentialsStore.Default.UpdateCurrentProject(selectedProject);
            }
        }
    }
}
