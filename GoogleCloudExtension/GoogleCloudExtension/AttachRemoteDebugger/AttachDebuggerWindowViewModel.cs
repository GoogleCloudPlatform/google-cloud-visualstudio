﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;

using System.Windows.Threading;

namespace GoogleCloudExtension.AttachRemoteDebugger
{
    /// <summary>
    /// View model to <seealso cref="AttachDebuggerWindowContent"/> user content.
    /// The view mode manages the attaching debugger steps.
    /// </summary>
    public class AttachDebuggerWindowViewModel : ViewModelBase
    {
        private readonly Instance _gceInstance;
        private IAttachDebuggerStep _currentStep;

        private ContentControl _content;
        private bool _isReady;
        private bool _showProgress;

        /// <summary>
        /// Whether the dialog is ready to process user input or not.
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            private set { SetValueAndRaise(ref _isReady, value); }
        }

        /// <summary>
        /// The command to execute when Cancel button is pressed.
        /// </summary>
        public ProtectedCommand CancelCommand { get; }

        /// <summary>
        /// The command to execute when OK button is pressed.
        /// </summary>
        public ProtectedCommand OKCommand { get; }

        /// <summary>
        /// Show or hide progress indicator
        /// </summary>
        public bool ShowProgressIndicator
        {
            get { return _showProgress; }
            private set { SetValueAndRaise(ref _showProgress, value); }
        }

        /// <summary>
        /// The content to display in the window.
        /// The content is the user control of current active <seealso cref="IAttachDebuggerStep"/>.
        /// </summary>
        public ContentControl Content
        {
            get { return _content; }
            private set { SetValueAndRaise(ref _content, value); }
        }

        public AttachDebuggerWindowViewModel(Instance gceInstance)
        {
            _gceInstance = gceInstance;
            OKCommand = new ProtectedCommand(taskHandler: () => ExceuteAsync(OnOKCommand), canExecuteCommand: false);
            CancelCommand = new ProtectedCommand(taskHandler: () => ExceuteAsync(OnCancelCommand), canExecuteCommand: false);
            GotoStep(null);     // Add initial step.
        }

        private async Task OnOKCommand()
        {
            if (_currentStep == null)
            {
                Debug.WriteLine("OnOKCommand, Unexpected error. _currentStep is null.");
                return;
            }
            IAttachDebuggerStep nextStep = await _currentStep.OnOKCommand();
            if (nextStep != null)
            {
                GotoStep(nextStep);
            }
        }

        private async Task OnCancelCommand()
        {
            if (_currentStep == null)
            {
                Debug.WriteLine("OnCancelCommand, Unexpected error. _currentStep is null.");
                return;
            }
            var nextStep = await _currentStep.OnCancelCommand();
            if (nextStep != null)
            {
                GotoStep(nextStep);
            }
        }

        private void OnStepPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender != _currentStep)
            {
                Debug.WriteLine("OnStepPropertyChanged, Unexpected error. _currentStep is not sender.");
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(IAttachDebuggerStep.IsCancelButtonEnabled):
                    CancelCommand.CanExecuteCommand = _currentStep.IsCancelButtonEnabled;
                    break;
                case nameof(IAttachDebuggerStep.IsOKButtonEnabled):
                    OKCommand.CanExecuteCommand = IsReady && _currentStep.IsOKButtonEnabled;
                    break;
            }
        }

        private void UpdateButtons()
        {
            CancelCommand.CanExecuteCommand = _currentStep.IsCancelButtonEnabled;
            OKCommand.CanExecuteCommand = IsReady && _currentStep.IsOKButtonEnabled;
        }

        private void GotoStep(IAttachDebuggerStep step)
        {
            if (_currentStep != null)
            {
                _currentStep.PropertyChanged -= OnStepPropertyChanged;
            }

            if (step != null)
            {
                Content = step.Content;
                step.PropertyChanged += OnStepPropertyChanged;

                Content.Loaded += OnContentLoaded;

                //                Content.Loaded += (sender, _) 
                //                    => ErrorHandlerUtils.HandleExceptionsAsync(
                //                        () => ExceuteAsync(
                //                            () => OnContentLoaded(sender)));
            }
            _currentStep = step;
        }

        private void OnContentLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            var action = new Action(() => ErrorHandlerUtils.HandleExceptionsAsync(
                () => ExceuteAsync(() => OnStart(sender))));
#pragma warning restore CS4014
            Content.Dispatcher.BeginInvoke(
                action,
                DispatcherPriority.ContextIdle, null);
        }

        private async Task OnStart(object sender)
        {
            if (_currentStep?.Content != sender)
            {
                Debug.WriteLine("OnStart, Unexpected error. Sender is not _currentStep.");
                return;
            }

            var nextStep = await _currentStep.OnStart();
            if (nextStep != null)
            {
                GotoStep(nextStep);
            }
        }

        private async Task ExceuteAsync(Func<Task> task)
        {
            IsReady = false;
            UpdateButtons();
            ShowProgressIndicator = true;
            try
            {
                await task();
            }
            finally
            {
                IsReady = true;
                ShowProgressIndicator = false;
                UpdateButtons();
            }
        }
    }
}
