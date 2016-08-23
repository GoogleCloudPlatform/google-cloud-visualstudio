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
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.WindowsCredentialsChooser
{
    public class WindowsCredentialsChooserViewModel : ViewModelBase
    {
        private static readonly IEnumerable<WindowsInstanceCredentials> s_addNewCredentials = new List<WindowsInstanceCredentials>
        {
            new WindowsInstanceCredentials
            {
                User = Resources.TerminalServerManagerNoCredentialsFoundMessage,
            }
        };

        private readonly Instance _instance;
        private readonly WindowsCredentialsChooserWindow _owner;
        private IEnumerable<WindowsInstanceCredentials> _instanceCredentials;
        private WindowsInstanceCredentials _currentCredentials;
        private bool _hasCredentials;

        public IEnumerable<WindowsInstanceCredentials> InstanceCredentials
        {
            get { return _instanceCredentials; }
            set { SetValueAndRaise(ref _instanceCredentials, value); }
        }

        public WeakCommand OkCommand { get; }

        public ICommand ManageCredentialsCommand { get; }

        public WindowsInstanceCredentials CurrentCredentials
        {
            get { return _currentCredentials; }
            set { SetValueAndRaise(ref _currentCredentials, value); }
        }

        public bool HasCredentials
        {
            get { return _hasCredentials; }
            set { SetValueAndRaise(ref _hasCredentials, value); }
        }

        public WindowsInstanceCredentials Result { get; private set; }

        public WindowsCredentialsChooserViewModel(Instance instance, WindowsCredentialsChooserWindow owner)
        {
            _instance = instance;
            _owner = owner;

            OkCommand = new WeakCommand(OnOkCommand, canExecuteCommand: false);
            ManageCredentialsCommand = new WeakCommand(OnManageCredentialsCommand);

            LoadCredentials();
        }

        private void OnManageCredentialsCommand()
        {
            ManageWindowsCredentialsWindow.PromptUser(_instance);
            LoadCredentials();
        }

        private void LoadCredentials()
        {
            InstanceCredentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);
            if (InstanceCredentials.Count() == 0)
            {
                InstanceCredentials = s_addNewCredentials;
                HasCredentials = false;
            }
            else
            {
                HasCredentials = true;
            }
            OkCommand.CanExecuteCommand = HasCredentials;
            if (CurrentCredentials == null)
            {
                CurrentCredentials = InstanceCredentials.FirstOrDefault();
            }
        }

        private void OnOkCommand()
        {
            Result = CurrentCredentials;
            _owner.Close();
        }
    }
}
