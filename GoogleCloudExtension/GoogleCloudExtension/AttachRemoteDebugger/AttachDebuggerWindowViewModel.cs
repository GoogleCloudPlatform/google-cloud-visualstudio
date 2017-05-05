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
using System.Diagnostics;
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
        private bool _isReady;
        private bool _showProgress;
        private bool _isOKButtonEnabled;
        private bool _isCancelButtonEnabled;

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

        public IAttachDebuggerStep CurrentStep
        {
            get { return _currentStep; }
            private set { SetValueAndRaise(ref _currentStep, value); }
        }

        public AttachDebuggerWindowViewModel(
            AttachDebuggerWindow owner,
            Instance gceInstance)
        {
            _owner = owner;
            _gceInstance = gceInstance;
            OKCommand = new ProtectedCommand(taskHandler: () => ExceuteAsync(OnOKCommand));
            CancelCommand = new ProtectedCommand(taskHandler: () => ExceuteAsync(OnCancelCommand));
            Start();
        }

        /// <summary>
        /// Start attaching debugger steps
        /// </summary>
        public void Start()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            OnStart();
#pragma warning restore CS4014
        }

        private async Task OnOKCommand()
        {
            if (_currentStep == null)
            {
                Debug.WriteLine("OnOKCommand _currentStep is null, not expected error.");
                return;
            }
            IAttachDebuggerStep nextStep = await _currentStep.OnOKCommand();
            ExecuteStep(nextStep);
        }

        private async Task OnCancelCommand()
        {
            if (_currentStep == null)
            {
                Debug.WriteLine("OnOKCommand _currentStep is null, not expected error.");
                return;
            }
            var nextStep = await _currentStep.OnCancelCommand();
            ExecuteStep(nextStep);
        }

        private void OnStart()
        {

            // TODO: Use check debugger port connectivity step to replace ExitStep
            ExecuteStep(ExitStep);
        }

        private void ExecuteStep(IAttachDebuggerStep step)
        {
            // If step is null, exit the loop, _currentStep will be visible to user.
            while (step != null && step != ExitStep)
            {
                if (_currentStep != null)
                {
                    // _currentStep.PropertyChanged -= OnStepPropertyChanged;
                }
                _currentStep = step;
                Content = step.Content;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Content.Loaded += (sender, e) => ErrorHandlerUtils.HandleExceptionsAsync(() => ExceuteAsync(() => OnContentLoaded(sender, e)));
#pragma warning restore CS4014
                // step.PropertyChanged += OnStepPropertyChanged;
                // step = await step.OnStart();
            }
            _currentStep = step;    // set _currentStep if step == ExitStep
        }

        private async Task OnContentLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_currentStep?.Content != sender)
            {
                Debug.WriteLine("OnContentLoaded sender is not _currentStep, not expected error.");
                return;
            }

            var nextStep = await _currentStep.OnStart();
            if (nextStep != null)
            {
                ExecuteStep(nextStep);
            }
        }

        private async Task ExceuteAsync(Func<Task> task)
        {
            IsReady = false;
            //UpdateButtons();
            try
            {
                await Task.Delay(5000);
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
                // UpdateButtons();
            }

            if (_currentStep == ExitStep)
            {
                _owner.Close();
            }
        }
    }
}
