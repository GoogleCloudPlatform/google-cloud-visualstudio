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
using GoogleCloudExtension.ShowPassword;
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
            }
        }

        /// <summary>
        /// Whether there is a user name, for validation.
        /// </summary>
        public bool HasUserName => !String.IsNullOrEmpty(UserName);

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

        public ResetPasswordViewModel(ResetPasswordWindow owner, Instance instance, string projectId)
        {
            _owner = owner;
            _instance = instance;
            _projectId = projectId;

            OkCommand = new WeakCommand(OnOkCommand);
            CancelCommand = new WeakCommand(OnCancelCommand);
        }

        private async void OnOkCommand()
        {
            if (!UserPromptUtils.YesNoPrompt(
                    $"Are you sure you want to reset the password for the user {UserName} in instance {_instance.Name}? This operation cannot be cancelled.",
                    "Reset Password"))
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
                    // TODO: Have a custom dialog with a link to the cloud sdk installation page.
                    if (!GCloudWrapper.IsGCloudCliInstalled())
                    {
                        UserPromptUtils.ErrorPrompt(
                            @"Ensure that the Cloud SDK is installed and available in the path, and that the ""beta"" component is installed.",
                            "Missing Cloud SDK");
                    }
                    else
                    {
                        UserPromptUtils.ErrorPrompt(
                            @"Please ensure that the ""beta"" is component installed, using ""gcloud components install beta"".",
                            "Missing Cloud SDK Component");
                    }
                    return;
                }

                var context = new Context
                {
                    CredentialsPath = CredentialsStore.Default.CurrentAccountPath,
                    ProjectId = _projectId,
                };
                var newCredentials = await GCloudWrapper.ResetWindowsCredentialsAsync(
                    instanceName: _instance.Name,
                    zoneName: _instance.GetZoneName(),
                    userName: _userName,
                    context: context);

                ResettingPassword = false;

                ShowPasswordWindow.PromptUser(
                    userName: UserName,
                    password: newCredentials.Password,
                    instanceName: _instance.Name);
            }
            catch (GCloudException ex)
            {
                UserPromptUtils.ErrorPrompt($"Failed to reset password for {_instance.Name}. {ex.Message}", "Reset Password");
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
    }
}
