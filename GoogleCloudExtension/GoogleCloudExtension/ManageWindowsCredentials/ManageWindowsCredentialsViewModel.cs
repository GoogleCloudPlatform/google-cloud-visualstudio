﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.AddWindowsCredential;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ProgressDialog;
using GoogleCloudExtension.ShowPassword;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ManageWindowsCredentials
{
    /// <summary>
    /// This class is the view model for the <seealso cref="ManageWindowsCredentialsWindow"/> dialog and
    /// implements all of the behaviors associated with it.
    /// </summary>
    public class ManageWindowsCredentialsViewModel : ViewModelBase
    {
        private readonly ManageWindowsCredentialsWindow _owner;
        private IEnumerable<WindowsInstanceCredentials> _credentials;
        private readonly Instance _instance;
        private WindowsInstanceCredentials _selectedCredentials;

        /// <summary>
        /// Command to execute when adding a new set of credentials.
        /// </summary>
        public ProtectedCommand AddCredentialsCommand { get; }

        /// <summary>
        /// Command to execute when deleting credentials.
        /// </summary>
        public ProtectedCommand DeleteCredentialsCommand { get; }

        /// <summary>
        /// Command to execute when showing the credentials to the user.
        /// </summary>
        public ProtectedCommand ShowCredentialsCommand { get; }

        /// <summary>
        /// The message to show at the top of the dialog.
        /// </summary>
        public string Message => String.Format(Resources.ManageWindowsCredentialsWindowMessage, _instance.Name);

        /// <summary>
        /// The currently selected credentials in the dialog.
        /// </summary>
        public WindowsInstanceCredentials SelectedCredentials
        {
            get { return _selectedCredentials; }
            set
            {
                SetValueAndRaise(ref _selectedCredentials, value);
                UpdateCommands();
            }
        }

        /// <summary>
        /// The list of credentials to display in the dialog.
        /// </summary>
        public IEnumerable<WindowsInstanceCredentials> CredentialsList
        {
            get { return _credentials; }
            set { SetValueAndRaise(ref _credentials, value); }
        }

        public ManageWindowsCredentialsViewModel(Instance instance, ManageWindowsCredentialsWindow owner)
        {
            _instance = instance;
            _owner = owner;

            CredentialsList = LoadCredentialsForInstance(instance);

            AddCredentialsCommand = new ProtectedCommand(OnAddCredentialsCommand);
            DeleteCredentialsCommand = new ProtectedCommand(OnDeleteCredentialsCommand, canExecuteCommand: false);
            ShowCredentialsCommand = new ProtectedCommand(OnShowCredentialsCommand, canExecuteCommand: false);
        }

        private IEnumerable<WindowsInstanceCredentials> LoadCredentialsForInstance(Instance instance)
        {
            return WindowsCredentialsStore.Default.GetCredentialsForInstance(instance);
        }

        private void OnShowCredentialsCommand()
        {
            ShowPasswordWindow.PromptUser(
                new ShowPasswordWindow.Options
                {
                    Title = String.Format(Resources.ShowPasswordWindowTitle, _instance.Name),
                    Password = SelectedCredentials.Password,
                    Message = String.Format(Resources.ShowPasswordMessage, SelectedCredentials.User)
                });
        }

        private void OnDeleteCredentialsCommand()
        {
            if (!UserPromptUtils.Default.ActionPrompt(
                    String.Format(Resources.ManageWindowsCredentialsDeleteCredentialsPromptMessage, SelectedCredentials.User),
                    Resources.ManageWindowsCredentialsDeleteCredentialsPromptTitle,
                    message: Resources.UiOperationCannotBeUndone,
                    actionCaption: Resources.UiDeleteButtonCaption))
            {
                return;
            }

            WindowsCredentialsStore.Default.DeleteCredentialsForInstance(_instance, SelectedCredentials);
            CredentialsList = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);
        }

        private async void OnAddCredentialsCommand()
        {
            var request = AddWindowsCredentialWindow.PromptUser(_instance);
            if (request == null)
            {
                return;
            }

            WindowsInstanceCredentials credentials;
            if (request.GeneratePassword)
            {
                var resetCredentialsTask = CreateOrResetCredentials(request.User);
                credentials = await ProgressDialogWindow.PromptUser(
                    resetCredentialsTask,
                    new ProgressDialogWindow.Options
                    {
                        Title = Resources.ResetPasswordProgressTitle,
                        Message = String.Format(Resources.ResetPasswordProgressMessage, request.User),
                        IsCancellable = false
                    });
                if (credentials != null)
                {
                    ShowPasswordWindow.PromptUser(
                        new ShowPasswordWindow.Options
                        {
                            Title = String.Format(Resources.ShowPasswordWindowTitle, _instance.Name),
                            Message = String.Format(Resources.ShowPasswordNewPasswordMessage, credentials.User),
                            Password = credentials.Password,
                        });
                }
            }
            else
            {
                credentials = new WindowsInstanceCredentials(request.User, request.Password);
            }

            if (credentials != null)
            {
                WindowsCredentialsStore.Default.AddCredentialsToInstance(_instance, credentials);
                CredentialsList = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);

                EventsReporterWrapper.ReportEvent(AddWindowsCredentialEvent.Create());
            }
        }

        private async Task<WindowsInstanceCredentials> CreateOrResetCredentials(string user)
        {
            try
            {
                Debug.WriteLine("The user requested the password to be generated.");
                if (!UserPromptUtils.Default.ActionPrompt(
                        prompt: String.Format(Resources.ResetPasswordConfirmationPromptMessage, user, _instance.Name),
                        title: Resources.ResetPasswordConfirmationPromptTitle,
                        message: Resources.UiOperationCannotBeUndone,
                        actionCaption: Resources.UiResetButtonCaption,
                        isWarning: true))
                {
                    Debug.WriteLine("The user cancelled resetting the password.");
                    return null;
                }

                Debug.WriteLine($"Resetting the password for the user {user}");
                if (!await GCloudWrapperUtils.VerifyGCloudDependencies())
                {
                    Debug.WriteLine("Gcloud dependencies not met, aborting change of password.");
                    return null;
                }

                var context = new GCloudContext();
                return await context.ResetWindowsCredentialsAsync(_instance.Name, _instance.GetZoneName(), user);
            }
            catch (GCloudException ex)
            {
                UserPromptUtils.Default.ErrorPrompt(
                    message: String.Format(Resources.ResetPasswordFailedPromptMessage, _instance.Name),
                    title: Resources.ResetPasswordConfirmationPromptTitle,
                    errorDetails: ex.Message);
                return null;
            }
        }

        private void UpdateCommands()
        {
            if (_selectedCredentials == null)
            {
                DeleteCredentialsCommand.CanExecuteCommand = false;
                ShowCredentialsCommand.CanExecuteCommand = false;
            }
            else
            {
                DeleteCredentialsCommand.CanExecuteCommand = true;
                ShowCredentialsCommand.CanExecuteCommand = true;
            }
        }
    }
}
