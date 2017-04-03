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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// The view model for the <seealso cref="PublishDialogWindowContent"/> control. Implements all of the interaction
    /// logic for the UI.
    /// </summary>
    public class PublishDialogWindowViewModel : ViewModelBase, IPublishDialog
    {
        private readonly PublishDialogWindow _owner;
        private readonly IParsedProject _project;
        private readonly Stack<IPublishDialogStep> _stack = new Stack<IPublishDialogStep>();
        private FrameworkElement _content;
        private bool _isReady = true;

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
        /// The command to execute when pressing the "Next" button.
        /// </summary>
        public ProtectedCommand NextCommand { get; }

        /// <summary>
        /// The command to execute when presing the "Publish" button.
        /// </summary>
        public ProtectedCommand PublishCommand { get; }

        /// <summary>
        /// Whether the dialog is ready to process user input (enabled) or not.
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            set { SetValueAndRaise(ref _isReady, value); }
        }

        /// <summary>
        /// The current <seealso cref="IPublishDialogStep"/> being shown.
        /// </summary>
        private IPublishDialogStep CurrentStep => _stack.Peek();

        public PublishDialogWindowViewModel(IParsedProject project, IPublishDialogStep initialStep, PublishDialogWindow owner)
        {
            _owner = owner;
            _project = project;

            PrevCommand = new ProtectedCommand(OnPrevCommand);
            NextCommand = new ProtectedCommand(OnNextCommand);
            PublishCommand = new ProtectedCommand(OnPublishCommand);

            PushStep(initialStep);
        }

        private void OnNextCommand()
        {
            var nextStep = CurrentStep.Next();
            PushStep(nextStep);
        }

        private void OnPrevCommand()
        {
            PopStep();
        }

        private void OnPublishCommand()
        {
            CurrentStep.Publish();
        }

        private void PushStep(IPublishDialogStep step)
        {
            RemoveStepEvents();
            _stack.Push(step);
            AddStepEvents();

            step.OnPushedToDialog(this);
            CurrentStepChanged();
        }

        private void AddStepEvents()
        {
            var top = _stack.Peek();
            top.CanGoNextChanged += OnCanGoNextChanged;
            top.CanPublishChanged += OnCanPublishChanged;
        }

        private void RemoveStepEvents()
        {
            if (_stack.Count > 0)
            {
                var top = _stack.Peek();
                top.CanGoNextChanged -= OnCanGoNextChanged;
                top.CanPublishChanged -= OnCanPublishChanged;
            }
        }

        private void PopStep()
        {
            RemoveStepEvents();
            _stack.Pop();
            AddStepEvents();

            CurrentStepChanged();
        }

        private void CurrentStepChanged()
        {
            Content = CurrentStep.Content;
            PrevCommand.CanExecuteCommand = _stack.Count > 1;
            NextCommand.CanExecuteCommand = CurrentStep.CanGoNext;
            PublishCommand.CanExecuteCommand = CurrentStep.CanPublish;
        }

        private void OnCanPublishChanged(object sender, EventArgs e)
        {
            PublishCommand.CanExecuteCommand = CurrentStep.CanPublish;
        }

        private void OnCanGoNextChanged(object sender, EventArgs e)
        {
            NextCommand.CanExecuteCommand = CurrentStep.CanGoNext;
        }

        #region IPublishDialog

        async void IPublishDialog.TrackTask(Task task)
        {
            try
            {
                IsReady = false;
                await task;
            }
            catch (Exception ex)
            {
                // This method is not interested at all in the exceptions thrown from the task. Other parts of the
                // extension will handle that error. But if we detect that there's a critical error we will let it
                // propagate.
                if (ErrorHandlerUtils.IsCriticalException(ex))
                {
                    throw;
                }
            }
            finally
            {
                IsReady = true;
            }
        }

        IParsedProject IPublishDialog.Project => _project;

        void IPublishDialog.NavigateToStep(IPublishDialogStep step)
        {
            PushStep(step);
        }

        void IPublishDialog.FinishFlow()
        {
            _owner.Close();
        }

        #endregion
    }
}
