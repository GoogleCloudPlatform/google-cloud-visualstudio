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
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ResetPassword;
using GoogleCloudExtension.ShowPassword;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtension.ManageWindowsCredentials
{
    public class ManageWindowsCredentialsViewModel : ViewModelBase
    {
        private readonly ManageWindowsCredentialsWindow _owner;
        private IEnumerable<WindowsInstanceCredentials> _credentials;
        private readonly Instance _instance;
        private WindowsInstanceCredentials _selectedCredentials;

        public WeakCommand AddCredentialsCommand { get; }

        public WeakCommand DeleteCredentialsCommand { get; }

        public WeakCommand ShowCredentialsCommand { get; }

        public string InstanceName => _instance.Name;

        public WindowsInstanceCredentials SelectedCredentials
        {
            get { return _selectedCredentials; }
            set
            {
                SetValueAndRaise(ref _selectedCredentials, value);
                UpdateCommands();
            }
        }

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

            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);
            DeleteCredentialsCommand = new WeakCommand(OnDeleteCredentialsCommand, canExecuteCommand: false);
            ShowCredentialsCommand = new WeakCommand(OnShowCredentialsCommand, canExecuteCommand: false);
        }

        private IEnumerable<WindowsInstanceCredentials> LoadCredentialsForInstance(Instance instance)
        {
            return WindowsCredentialsStore.Default.GetCredentialsForInstance(instance);
        }

        private void OnShowCredentialsCommand()
        {
            ShowPasswordWindow.PromptUser(
                userName: SelectedCredentials.User,
                password: SelectedCredentials.Password,
                instanceName: _instance.Name);
        }

        private void OnDeleteCredentialsCommand()
        {
            if (!UserPromptUtils.YesNoPrompt(
                String.Format(Resources.ManageWindowsCredentialsDeleteCredentialsPromptMessage, SelectedCredentials.User),
                Resources.ManageWindowsCredentialsDeleteCredentialsPromptTitle))
            {
                return;
            }

            WindowsCredentialsStore.Default.DeleteCredentialsForInstance(_instance, SelectedCredentials);
            CredentialsList = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);
        }

        private void OnAddCredentialsCommand()
        {
            var credentials = ResetPasswordWindow.PromptUser(_instance, CredentialsStore.Default.CurrentProjectId);
            if (credentials != null)
            {
                WindowsCredentialsStore.Default.AddCredentialsToInstance(_instance, credentials);
                CredentialsList = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);
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
