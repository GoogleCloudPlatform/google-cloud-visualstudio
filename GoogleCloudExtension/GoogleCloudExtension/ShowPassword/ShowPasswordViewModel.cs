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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogleCloudExtension.ShowPassword
{
    /// <summary>
    /// View model for the dialog that presents the password to the user.
    /// </summary>
    public class ShowPasswordViewModel : ViewModelBase
    {
        private readonly ShowPasswordWindow.Options _options;
        private bool _showCopyFeedback;

        /// <summary>
        /// The password to show.
        /// </summary>
        public string Password => _options.Password;

        /// <summary>
        /// The message to display in the dialog.
        /// </summary>
        public string Message => _options.Message;

        /// <summary>
        /// The command to execute to copy the password to the clipboard.
        /// </summary>
        public ICommand CopyCommand { get; }

        /// <summary>
        /// Whether to show the copy feedback or not.
        /// </summary>
        public bool ShowCopyFeedback
        {
            get { return _showCopyFeedback; }
            set { SetValueAndRaise(ref _showCopyFeedback, value); }
        }

        public ShowPasswordViewModel(ShowPasswordWindow.Options options)
        {
            _options = options;

            CopyCommand = new ProtectedCommand(OnCopyCommand);
        }

        private async void OnCopyCommand()
        {
            try
            {
                Clipboard.SetText(Password);

                if (!ShowCopyFeedback)
                {
                    ShowCopyFeedback = true;
                    await Task.Delay(2000);
                    ShowCopyFeedback = false;
                }
            }
            catch
            {
                Debug.WriteLine("Failed to copy the string to the clipboard.");
                UserPromptUtils.Default.ErrorPrompt(Resources.ShowPasswordCopyFailedMessage, Resources.ShowPasswordCopyFailedTitle);
            }
        }
    }
}
