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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.LinkPrompt;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.ResetPassword
{
    /// <summary>
    /// Viewmodel for the reset password dialog.
    /// </summary>
    public class ResetPasswordViewModel : ViewModelBase
    {
        private string _userName;
        private string _password;
        private bool _generatePassword = true;
        private bool _manualPassword;
        private bool _resettingPassword;
        private readonly ResetPasswordWindow _owner;
        private readonly Instance _instance;
        private readonly string _projectId;

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
                UpdateOkCommand();
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
                UpdateOkCommand();
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
                UpdateOkCommand();
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
                UpdateOkCommand();
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
        /// Whether the dialog is in the busy state.
        /// </summary>
        public bool ResettingPassword
        {
            get { return _resettingPassword; }
            set
            {
                SetValueAndRaise(ref _resettingPassword, value);
                RaisePropertyChanged(nameof(IsNotResettingPassword));
            }
        }

        /// <summary>
        /// Negation of the ResettingPassword.
        /// </summary>
        public bool IsNotResettingPassword => !ResettingPassword;

        /// <summary>
        /// The command to execute to accept the changes.
        /// </summary>
        public WeakCommand OkCommand { get; }

        /// <summary>
        /// The command to exectue to cancel the changes.
        /// </summary>
        public WeakCommand CancelCommand { get; }

        public WindowsInstanceCredentials Result { get; private set; }

        public ResetPasswordViewModel(ResetPasswordWindow owner, Instance instance, string projectId)
        {
            _owner = owner;
            _instance = instance;
            _projectId = projectId;

            OkCommand = new WeakCommand(OnOkCommand, canExecuteCommand: false);
            CancelCommand = new WeakCommand(OnCancelCommand);
        }

        private async void OnOkCommand()
        {
            if (ManualPassword)
            {
                Debug.WriteLine("The user is supplying the password.");
                Result = new WindowsInstanceCredentials
                {
                    User = UserName,
                    Password = Password
                };
                _owner.Close();
                return;
            }

            Debug.WriteLine("The user requested the password to be generated.");
            if (!UserPromptUtils.YesNoPrompt(
                    String.Format(Resources.ResetPasswordConfirmationPromptMessage, UserName, _instance.Name),
                    Resources.ResetPasswordConfirmationPromptTitle))
            {
                Debug.WriteLine("The user cancelled resetting the password.");
                return;
            }

            try
            {
                Debug.WriteLine($"Resetting the password for the user {UserName}");

                ResettingPassword = true;

                // The operation cannot be cancelled once it started, so we have to disable the buttons while
                // it is in flight.
                OkCommand.CanExecuteCommand = false;
                CancelCommand.CanExecuteCommand = false;
                _owner.IsCloseButtonEnabled = false;

                // Check that gcloud is in the right state to invoke the reset credentials method.
                if (!await GCloudWrapper.CanUseResetWindowsCredentialsAsync())
                {
                    if (!GCloudWrapper.IsGCloudCliInstalled())
                    {
                        LinkPromptDialogWindow.PromptUser(
                            Resources.ResetPasswordMissingGcloudTitle,
                            Resources.ResetPasswordGcloudMissingMessage,
                            new LinkInfo(link: "https://cloud.google.com/sdk/", caption: Resources.ResetPasswordGcloudLinkCaption));
                    }
                    else
                    {
                        UserPromptUtils.ErrorPrompt(
                            message: Resources.ResetPasswordGcloudMissingBetaMessage,
                            title: Resources.ResetPasswordGcloudMissingComponentTitle);
                    }
                    return;
                }

                var context = new Context
                {
                    CredentialsPath = CredentialsStore.Default.CurrentAccountPath,
                    ProjectId = _projectId,
                    AppName = GoogleCloudExtensionPackage.ApplicationName,
                    AppVersion = GoogleCloudExtensionPackage.ApplicationVersion,
                };
                Result = await GCloudWrapper.ResetWindowsCredentialsAsync(
                    instanceName: _instance.Name,
                    zoneName: _instance.GetZoneName(),
                    userName: _userName,
                    context: context);

                ResettingPassword = false;
            }
            catch (GCloudException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    String.Format(Resources.ResetPasswordFailedPromptMessage, _instance.Name, ex.Message),
                    Resources.ResetPasswordConfirmationPromptTitle);
            }
            finally
            {
                _owner.Close();
            }
        }

        private void OnCancelCommand()
        {
            _owner.Close();
        }

        private void UpdateOkCommand()
        {
            // The Ok command should be enabled if the user name was specified and if (optionally)
            // the password is specified.
            OkCommand.CanExecuteCommand = HasUserName && (!ManualPassword || HasPassword);
        }
    }
}
