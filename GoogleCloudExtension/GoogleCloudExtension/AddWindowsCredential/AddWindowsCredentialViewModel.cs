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
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.AddWindowsCredential
{
    /// <summary>
    /// Viewmodel for the AddWindowsCredential dialog.
    /// </summary>
    public class AddWindowsCredentialViewModel : ValidatingViewModelBase
    {
        // These are the chars that must not be present in a Windows username.
        // This list was obtained from https://technet.microsoft.com/en-us/library/bb726984.aspx
        private const string UserNameInvalidChars = @"""/\[]:;|=,+*?<>";

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
                SetAndRaiseWithValidation(out _userName, value, ValidateUserName(value));
                RaisePropertyChanged(nameof(HasUserName));
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
                SetValueAndRaise(out _password, value);
                RaisePropertyChanged(nameof(HasPassword));
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
                SetValueAndRaise(out _generatePassword, value);
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
                SetValueAndRaise(out _manualPassword, value);
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
        public ICommand SaveCommand { get; }

        public AddWindowsCredentialResult Result { get; private set; }

        public AddWindowsCredentialViewModel(AddWindowsCredentialWindow owner, Instance instance)
        {
            _owner = owner;
            _instance = instance;

            SaveCommand = new ProtectedCommand(OnSaveCommand);
        }

        private void OnSaveCommand()
        {
            if (!Validate())
            {
                return;
            }

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

        private IEnumerable<ValidationResult> ValidateUserName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.AddWindowsCredentialValidationEmptyUser));
            }
            else
            {
                List<char> invalidChars = value.Intersect(UserNameInvalidChars).ToList();
                if (invalidChars.Count != 0)
                {
                    yield return StringValidationResult.FromResource(
                        nameof(Resources.AddWindowsCredentialValidationInvalidChars), string.Concat(invalidChars));
                }
            }
        }

        private bool Validate()
        {
            if (String.IsNullOrEmpty(UserName))
            {
                UserPromptUtils.ErrorPrompt(
                    Resources.AddWindowsCredentialValidationEmptyUser,
                    Resources.AddWindowsCredentialValidationErrorTtitle);
                return false;
            }

            var invalidChars = UserName.Intersect(UserNameInvalidChars).ToArray();
            if (invalidChars.Length > 0)
            {
                UserPromptUtils.ErrorPrompt(
                    String.Format(Resources.AddWindowsCredentialValidationInvalidChars, new string(invalidChars)),
                    Resources.AddWindowsCredentialValidationErrorTtitle);
                return false;
            }

            if (ManualPassword && String.IsNullOrEmpty(Password))
            {
                UserPromptUtils.ErrorPrompt(
                    Resources.AddWindowsCredentialValidationEmptyPassword,
                    Resources.AddWindowsCredentialValidationErrorTtitle);
                return false;
            }

            // Valid entry.
            return true;
        }
    }
}
