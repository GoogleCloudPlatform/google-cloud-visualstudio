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
using System.Windows;
using System.Windows.Input;
using System;
using System.Windows.Media;

namespace GoogleCloudExtension.ShowPassword
{
    /// <summary>
    /// View model for the dialog that presents the password to the user.
    /// </summary>
    public class ShowPasswordViewModel : ViewModelBase
    {
        private const string ShowPasswordIconPath = "ShowPassword/Resources/visibility.png";
        private const string HidePasswordIconPath = "ShowPassword/Resources/visibility_off.png";

        private static readonly Lazy<ImageSource> s_showPasswordIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ShowPasswordIconPath));
        private static readonly Lazy<ImageSource> s_hidePasswordIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(HidePasswordIconPath));

        private readonly ShowPasswordWindow _owner;
        private bool _revealPassword;

        /// <summary>
        /// The user name for the credentials.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// The password to show.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Wether to show or not the password to the user.
        /// </summary>
        public bool RevealPassword
        {
            get { return _revealPassword; }
            set
            {
                SetValueAndRaise(ref _revealPassword, value);
                RaisePropertyChanged(nameof(HidePassword));
                RaisePropertyChanged(nameof(ShowPasswordIcon));
            }
        }

        public bool HidePassword => !RevealPassword;

        public ImageSource ShowPasswordIcon => RevealPassword ? s_hidePasswordIcon.Value : s_showPasswordIcon.Value;
      
        /// <summary>
        /// The name of the instance for which the credentials are valid.
        /// </summary>
        public string InstanceName { get; }

        /// <summary>
        /// The command to execute to accept and close the window.
        /// </summary>
        public ICommand OkCommand { get; }

        public ICommand TogglePasswordCommand { get; }

        /// <summary>
        /// The command to execute to copy the password to the clipboard.
        /// </summary>
        public ICommand CopyCommand { get; }

        public ShowPasswordViewModel(ShowPasswordWindow owner, string userName, string password, string instanceName)
        {
            _owner = owner;

            UserName = userName;
            Password = password;
            InstanceName = instanceName;

            OkCommand = new WeakCommand(OnOkCommand);
            TogglePasswordCommand = new WeakCommand(OnTogglePasswordCommand);
            CopyCommand = new WeakCommand(OnCopyCommand);
        }

        private void OnCopyCommand()
        {
            Clipboard.SetText(Password);
        }

        private void OnTogglePasswordCommand()
        {
            RevealPassword = !RevealPassword;
        }

        private void OnOkCommand()
        {
            _owner.Close();
        }
    }
}
