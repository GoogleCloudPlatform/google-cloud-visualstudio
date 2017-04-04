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

using GoogleCloudExtension.Utils;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.UserPrompt
{
    /// <summary>
    /// This class is the view model for the user prompt dialog.
    /// </summary>
    public class UserPromptWindowViewModel : ViewModelBase
    {
        private readonly UserPromptWindow _owner;
        private readonly UserPromptWindow.Options _options;

        /// <summary>
        /// The prompt to use in the dialog.
        /// </summary>
        public string Prompt => _options.Prompt?.Replace(@"\r\n", Environment.NewLine);

        /// <summary>
        /// The message to display.
        /// </summary>
        public string Message => _options.Message?.Replace(@"\r\n", Environment.NewLine);

        /// <summary>
        /// The error details to show in the dialog.
        /// </summary>
        public string ErrorDetails => _options.ErrorDetails;

        /// <summary>
        /// Returns true if there are error details to show.
        /// </summary>
        public bool HasErrorDetails => ErrorDetails != null;

        /// <summary>
        /// The icon to use for the dialog.
        /// </summary>
        public ImageSource Icon => _options.Icon;

        /// <summary>
        /// Whether there's an icon to show or no.
        /// </summary>
        public bool HasIcon => Icon != null;

        /// <summary>
        /// The command to execute in the action button.
        /// </summary>
        public ICommand ActionCommand { get; }

        /// <summary>
        /// The caption for the action button.
        /// </summary>
        public string ActionButtonCaption => _options.ActionButtonCaption;

        /// <summary>
        /// Returns whether there is an action button.
        /// </summary>
        public bool HasActionButton => !String.IsNullOrEmpty(_options.ActionButtonCaption);

        /// <summary>
        /// Negation of <seealso cref="HasActionButton"/> which helps with bindings in the Xaml.
        /// </summary>
        public bool DoesNotHaveActionButton => !HasActionButton;

        /// <summary>
        /// The caption for the cancel button.
        /// </summary>
        public string CancelButtonCaption => _options.CancelButtonCaption;

        /// <summary>
        /// The final result of the dialog, true if the user pressed the action button, false if the
        /// user cancelled out, either by pressing the cancel button or closing the dialog.
        /// </summary>
        public bool Result { get; private set; }

        public UserPromptWindowViewModel(UserPromptWindow owner, UserPromptWindow.Options options)
        {
            _owner = owner;
            _options = options;

            ActionCommand = new ProtectedCommand(OnActionCommand);
        }

        private void OnActionCommand()
        {
            Result = true;
            _owner.Close();
        }
    }
}
