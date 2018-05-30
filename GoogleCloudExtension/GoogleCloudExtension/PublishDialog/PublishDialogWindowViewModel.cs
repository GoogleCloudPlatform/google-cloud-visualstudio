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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog.Steps;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// The view model for the <seealso cref="PublishDialogWindowContent"/> control. Implements all of the interaction
    /// logic for the UI.
    /// </summary>
    public class PublishDialogWindowViewModel : ViewModelBase, IPublishDialog, INotifyDataErrorInfo
    {
        private readonly Stack<IStepContent<IPublishDialogStep>> _stack = new Stack<IStepContent<IPublishDialogStep>>();
        private FrameworkElement _content;
        private IProtectedCommand _publishCommand;
        private readonly Action _closeWindow;

        /// <summary>
        /// The content to display to the user, the content of the active <seealso cref="IPublishDialogStep"/> .
        /// </summary>
        public FrameworkElement Content
        {
            get { return _content; }
            set { SetValueAndRaise(ref _content, value); }
        }

        /// <summary>
        /// The command to execute when pressing the "Prev" button.
        /// </summary>
        public ProtectedCommand PrevCommand { get; }

        /// <summary>
        /// The command to execute when presing the "Publish" button.
        /// </summary>
        public IProtectedCommand PublishCommand
        {
            get => _publishCommand;
            set => SetValueAndRaise(ref _publishCommand, value);
        }

        /// <summary>
        /// The current <seealso cref="IPublishDialogStep"/> being shown.
        /// </summary>
        private IPublishDialogStep CurrentStep => _stack.Peek().ViewModel;

        public PublishDialogWindowViewModel(IParsedProject project, Action closeWindow)
        {
            _closeWindow = closeWindow;
            Project = project;

            PrevCommand = new ProtectedCommand(OnPrevCommand);

            PushStep(new ChoiceStepContent(this));
        }

        private void OnPrevCommand()
        {
            PopStep();
        }

        private void PushStep(IStepContent<IPublishDialogStep> nextStepContent)
        {
            IStepContent<IPublishDialogStep> oldStepContent = _stack.Count > 0 ? _stack.Peek() : null;
            _stack.Push(nextStepContent);
            Content = nextStepContent as FrameworkElement;
            ChangeCurrentStep(oldStepContent?.ViewModel);
        }

        private void PopStep()
        {
            IStepContent<IPublishDialogStep> oldStepContent = _stack.Pop();
            Content = _stack.Peek() as FrameworkElement;
            ChangeCurrentStep(oldStepContent.ViewModel);
        }

        private void ChangeCurrentStep(IPublishDialogStep oldStep)
        {
            if (oldStep != null)
            {
                RemoveStepEvents(oldStep);
                oldStep.OnNotVisible();
            }

            CurrentStep.OnVisibleAsync();
            AddStepEvents(CurrentStep);
            PrevCommand.CanExecuteCommand = _stack.Count > 1;
            PublishCommand = CurrentStep.PublishCommand;
        }

        private void AddStepEvents(IPublishDialogStep dialogStep)
        {
            dialogStep.ErrorsChanged += OnErrorsChanged;
        }

        private void RemoveStepEvents(IPublishDialogStep dialogStep)
        {
            dialogStep.ErrorsChanged -= OnErrorsChanged;
        }

        private void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            ErrorsChanged?.Invoke(sender, e);
        }

        #region IPublishDialog

        /// <inheritdoc />
        public IParsedProject Project { get; }

        public void NavigateToStep(IStepContent<IPublishDialogStep> step)
        {
            PushStep(step);
        }

        /// <inheritdoc />
        public void FinishFlow()
        {
            FlowFinished?.Invoke(this, EventArgs.Empty);
            _closeWindow();
        }

        /// <inheritdoc />
        public event EventHandler FlowFinished;

        #endregion

        #region INotifyDataErrorInfo

        /// <inheritdoc />
        public IEnumerable GetErrors(string propertyName)
        {
            return CurrentStep.GetErrors(propertyName);
        }

        /// <inheritdoc />
        public bool HasErrors => CurrentStep.HasErrors;

        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion
    }
}
