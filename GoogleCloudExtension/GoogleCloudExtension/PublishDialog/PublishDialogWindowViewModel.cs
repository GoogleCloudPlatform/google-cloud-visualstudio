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

using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog.Steps;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// The view model for the <seealso cref="PublishDialogWindowContent"/> control. Implements all of the interaction
    /// logic for the UI.
    /// </summary>
    public class PublishDialogWindowViewModel : ViewModelBase, IPublishDialog, INotifyDataErrorInfo
    {
        private readonly Stack<IStepContent<IPublishDialogStep>> _stack = new Stack<IStepContent<IPublishDialogStep>>();
        private readonly Action _closeWindow;
        private AsyncProperty _asyncAction = new AsyncProperty();

        public AsyncProperty AsyncAction
        {
            get => _asyncAction;
            set => SetValueAndRaise(ref _asyncAction, value);
        }

        /// <summary>
        /// The content to display to the user, the content of the active <seealso cref="IPublishDialogStep"/> .
        /// </summary>
        public IStepContent<IPublishDialogStep> Content => _stack.Peek();

        /// <summary>
        /// The command to execute when pressing the "Prev" button.
        /// </summary>
        public ProtectedCommand PrevCommand { get; }

        /// <summary>
        /// The current <seealso cref="IPublishDialogStep"/> being shown.
        /// </summary>
        private IPublishDialogStep CurrentStep => Content.ViewModel;

        public PublishDialogWindowViewModel(IParsedDteProject project, Action closeWindow)
        {
            _closeWindow = closeWindow;
            Project = project;

            PrevCommand = new ProtectedCommand(OnPrevCommand);

            var initialStep = new ChoiceStepContent(this);
            PushStep(initialStep, null);
        }

        private void OnPrevCommand()
        {
            PopStep();
        }

        private void PushStep(
            IStepContent<IPublishDialogStep> nextStepContent,
            IStepContent<IPublishDialogStep> previousStepContent)
        {
            _stack.Push(nextStepContent);
            ChangeCurrentStep(previousStepContent?.ViewModel);
        }

        public void PopStep()
        {
            IStepContent<IPublishDialogStep> oldStepContent = _stack.Pop();
            ChangeCurrentStep(oldStepContent.ViewModel);
        }

        private void ChangeCurrentStep(IPublishDialogStep oldStep)
        {
            if (oldStep != null)
            {
                RemoveStepEvents(oldStep);
                oldStep.OnNotVisible();
            }

            CurrentStep.OnVisible(oldStep);
            AddStepEvents(CurrentStep);
            PrevCommand.CanExecuteCommand = _stack.Count > 1;
            RaisePropertyChanged(nameof(Content));
        }

        private void AddStepEvents(INotifyDataErrorInfo dialogStep)
        {
            dialogStep.ErrorsChanged += OnErrorsChanged;
        }

        private void RemoveStepEvents(INotifyDataErrorInfo dialogStep)
        {
            dialogStep.ErrorsChanged -= OnErrorsChanged;
        }

        #region IPublishDialog

        public IParsedDteProject Project { get; }

        public void NavigateToStep(IStepContent<IPublishDialogStep> step) => PushStep(step, Content);

        /// <summary>
        /// Called from a step that wants to finish the flow, or when the dialog is closed.
        /// </summary>
        public void FinishFlow()
        {
            FlowFinished?.Invoke(this, EventArgs.Empty);
            _closeWindow();
        }

        /// <summary>
        /// Event raised when <see cref="IPublishDialog.FinishFlow"/> is called.
        /// Gives the step an opportunity to cleanup.
        /// </summary>
        public event EventHandler FlowFinished;

        /// <summary>
        /// Makes the dialog look "busy" as long as the <paramref name="task"/> is running.
        /// </summary>
        /// <param name="task">The task to track.</param>
        public void TrackTask(Task task) => AsyncAction = new AsyncProperty(task);

        #endregion

        /// <summary>Gets the validation errors for a specified property.</summary>
        /// <returns>The validation errors for the property.</returns>
        /// <param name="propertyName">
        /// The name of the property to retrieve validation errors for.
        /// If null or <see cref="string.Empty" /> retrieve all dialog errors.
        /// </param>
        public IEnumerable GetErrors(string propertyName) => CurrentStep.GetErrors(propertyName);

        /// <summary>Indicates whether the dialog has validation errors. </summary>
        /// <returns>true if the dialog currently has validation errors; false otherwise.</returns>
        public bool HasErrors => CurrentStep.HasErrors;

        /// <summary>Occurs when the validation errors have changed.</summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged = (sender, args) => { };

        private void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e) => ErrorsChanged(this, e);
    }
}
