// Copyright 2017 Google Inc. All Rights Reserved.
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
using static GoogleCloudExtension.AttachRemoteDebugger.AttachDebuggerStepBase;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachRemoteDebugger
{
    /// <summary>
    /// View model to <seealso cref="AttachDebuggerWindowContent"/> user content.
    /// The view mode manages the attaching debugger steps.
    /// </summary>
    public class AttachDebuggerWindowViewModel : ViewModelBase
    {
        private readonly AttachDebuggerWindow _owner;

        private Instance _gceInstance;
        private IAttachDebuggerStep _currentStep;

        private ContentControl _content;
        private bool _isReady = false;
        private bool _isOKButtonEnabled = false;
        private bool _isCancelButtonEnabled = false;

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
        /// Enable/Disable OK button
        /// </summary>
        public bool IsOKButtonEnabled
        {
            get { return _isOKButtonEnabled; }
            private set { SetValueAndRaise(ref _isOKButtonEnabled, value); }
        }

        /// <summary>
        /// Enable/Disable Cancel button
        /// </summary>
        public bool IsCancelButtonEnabled
        {
            get { return _isCancelButtonEnabled; }
            private set { SetValueAndRaise(ref _isCancelButtonEnabled, value); }
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

        public AttachDebuggerWindowViewModel(
            AttachDebuggerWindow owner,
            Instance gceInstance)
        {
            _owner = owner;
            _gceInstance = gceInstance;
            OKCommand = new ProtectedCommand(taskHandler: () => ExceuteAsync(OnOKCommand));
            CancelCommand = new ProtectedCommand(taskHandler: () => ExceuteAsync(OnCancelCommand));
        }

        /// <summary>
        /// Start attaching debugger steps
        /// </summary>
        public void Start()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ErrorHandlerUtils.HandleExceptionsAsync(() => ExceuteAsync(OnStart));
#pragma warning restore CS4014
        }

        private void OnStepPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IAttachDebuggerStep.IsCancelButtonEnabled):
                    IsCancelButtonEnabled = IsReady && _currentStep?.IsCancelButtonEnabled == true;
                    break;
                case nameof(IAttachDebuggerStep.IsOKButtonEnabled):
                    IsOKButtonEnabled = IsReady && _currentStep?.IsOKButtonEnabled == true;
                    break;
            }
        }

        private void UpdateButtons()
        {
            IsOKButtonEnabled = IsReady && _currentStep?.IsOKButtonEnabled == true;
            IsCancelButtonEnabled = IsReady && _currentStep?.IsCancelButtonEnabled == true;
        }

        private async Task OnOKCommand()
        {
            IAttachDebuggerStep nextStep = await _currentStep?.OnOKCommand();
            await ExecuteStep(nextStep);
        }

        private async Task OnCancelCommand()
        {
            var step = _currentStep?.OnCancelCommand();
            await ExecuteStep(step);
        }

        private async Task OnStart()
        {
            if (String.IsNullOrWhiteSpace(_gceInstance.GetPublicIpAddress()))
            {
                UserPromptUtils.OkPrompt(
                    message: Resources.AttachDebuggerAddPublicIpAddressMessage,
                    title: Resources.uiDefaultPromptTitle);
                _currentStep = ExitStep;
                return;
            }

            // TODO: Use check debugger port connectivity step to replace ExitStep
            await ExecuteStep(ExitStep);
        }

        private async Task ExecuteStep(IAttachDebuggerStep step)
        {
            // If step is null, exit the loop, _currentStep will be visible to user.
            while (step != null && step != ExitStep)
            {
                if (_currentStep != null)
                {
                    _currentStep.PropertyChanged -= OnStepPropertyChanged;
                }
                _currentStep = step;
                Content = step.Content;
                step.PropertyChanged += OnStepPropertyChanged;
                step = await step.OnStart();
            }
            _currentStep = step;    // set _currentStep if step == ExitStep
        }

        private async Task ExceuteAsync(Func<Task> task)
        {
            IsReady = false;
            UpdateButtons();
            try
            {
                await task();
            }
            catch (Exception ex) when
                (ex is DataSourceException ||
                 ex is AttachDebuggerException)
            {
                // TODO: Add help page or generic error page
                if (ex is AggregateException)
                {
                    UserPromptUtils.ErrorPrompt(
                        title: Resources.ExceptionPromptTitle,
                        message: String.Format(Resources.ExceptionPromptMessage, ex.InnerException.Message),
                        errorDetails: ex.InnerException.ToString());
                }
                else
                {
                    UserPromptUtils.ErrorPrompt(
                        title: Resources.ExceptionPromptTitle,
                        message: String.Format(Resources.ExceptionPromptMessage, ex.Message),
                        errorDetails: ex.ToString());
                }
                _currentStep = ExitStep;
            }
            finally
            {
                IsReady = true;
                UpdateButtons();
            }

            if (_currentStep == ExitStep)
            {
                _owner.Close();
            }
        }
    }
}
