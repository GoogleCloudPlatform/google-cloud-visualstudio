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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.AddWindowsCredential
{
    /// <summary>
    /// Viewmodel for the AddWindowsCredential dialog.
    /// </summary>
    public class AddWindowsCredentialViewModel : ViewModelBase
    {
        private string _userName;
        private string _password;
        private bool _generatePassword = true;
        private bool _manualPassword;
        private readonly AddWindowsCredentialWindow _owner;
        private readonly Instance _instance;

        /// <summary>
        /// The username requested by the user.
        /// </summary>
        public string UserName
        {
            get { return _userName; }
            set
            {
                SetValueAndRaise(ref _userName, value);
                RaisePropertyChanged(nameof(HasUserName));
                UpdateSaveCommand();
            }
        }

        /// <summary>
        /// The password the user provided manually.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                SetValueAndRaise(ref _password, value);
                RaisePropertyChanged(nameof(HasPassword));
                UpdateSaveCommand();
            }
        }

        /// <summary>
        /// Whether the user opted for the password to be generated.
        /// </summary>
        public bool GeneratePassword
        {
            get { return _generatePassword; }
            set
            {
                SetValueAndRaise(ref _generatePassword, value);
                UpdateSaveCommand();
            }
        }

        /// <summary>
        /// Whether the user opted for providing a manual password.
        /// </summary>
        public bool ManualPassword
        {
            get { return _manualPassword; }
            set
            {
                SetValueAndRaise(ref _manualPassword, value);
                UpdateSaveCommand();
            }
        }

        /// <summary>
        /// Whether there is a user name, for validation.
        /// </summary>
        public bool HasUserName => !String.IsNullOrEmpty(UserName);

        /// <summary>
        /// Whether there is a password.
        /// </summary>
        public bool HasPassword => !String.IsNullOrEmpty(Password);

        /// <summary>
        /// The command to execute to accept the changes.
        /// </summary>
        public ProtectedCommand SaveCommand { get; }

        public AddWindowsCredentialResult Result { get; private set; }

        public AddWindowsCredentialViewModel(AddWindowsCredentialWindow owner, Instance instance)
        {
            _owner = owner;
            _instance = instance;

            SaveCommand = new ProtectedCommand(OnSaveCommand, canExecuteCommand: false);
        }

        private void OnSaveCommand()
        {
            if (ManualPassword)
            {
                Debug.WriteLine("The user is supplying the password.");
                Result = new AddWindowsCredentialResult(UserName, Password);
            }
            else
            {
                Debug.WriteLine("The user is requesting the password to be generated.");
                Result = new AddWindowsCredentialResult(UserName);
            }

            _owner.Close();
            return;
        }

        private void UpdateSaveCommand()
        {
            // The Ok command should be enabled if the user name was specified and if (optionally)
            // the password is specified.
            SaveCommand.CanExecuteCommand = HasUserName && (!ManualPassword || HasPassword);
        }
    }
}
