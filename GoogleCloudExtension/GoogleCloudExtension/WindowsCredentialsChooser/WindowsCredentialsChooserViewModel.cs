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
                User = Resources.WindowsCredentialsChooserNoCredentialsFoundMessage,
            }
        };

        private readonly Instance _instance;
        private readonly WindowsCredentialsChooserWindow.Options _options;
        private readonly WindowsCredentialsChooserWindow _owner;
        private IEnumerable<WindowsInstanceCredentials> _instanceCredentials;
        private WindowsInstanceCredentials _currentCredentials;
        private bool _hasCredentials;

        /// <summary>
        /// The message to dislay in the dialog.
        /// </summary>
        public string Message => _options.Message;

        /// <summary>
        /// The caption for the aciton button.
        /// </summary>
        public string ActionButtonCaption => _options.ActionButtonCaption;

        /// <summary>
        /// The list of credentials for the current instance.
        /// </summary>
        public IEnumerable<WindowsInstanceCredentials> InstanceCredentials
        {
            get { return _instanceCredentials; }
            set { SetValueAndRaise(out _instanceCredentials, value); }
        }

        /// <summary>
        /// The selected credentials.
        /// </summary>
        public WindowsInstanceCredentials CurrentCredentials
        {
            get { return _currentCredentials; }
            set { SetValueAndRaise(out _currentCredentials, value); }
        }

        /// <summary>
        /// The command to execute in the action button.
        /// </summary>
        public ProtectedCommand ActionCommand { get; }

        /// <summary>
        /// The command to exectue from the manage credentials button.
        /// </summary>
        public ICommand ManageCredentialsCommand { get; }

        /// <summary>
        /// Wether there are credentials for the instance.
        /// </summary>
        public bool HasCredentials
        {
            get { return _hasCredentials; }
            set { SetValueAndRaise(out _hasCredentials, value); }
        }

        /// <summary>
        /// The selected set of credentials, or null if the user cancelled out.
        /// </summary>
        public WindowsInstanceCredentials Result { get; private set; }

        public WindowsCredentialsChooserViewModel(
            Instance instance,
            WindowsCredentialsChooserWindow.Options options,
            WindowsCredentialsChooserWindow owner)
        {
            _instance = instance;
            _options = options;
            _owner = owner;

            ActionCommand = new ProtectedCommand(OnActionCommand, canExecuteCommand: false);
            ManageCredentialsCommand = new ProtectedCommand(OnManageCredentialsCommand);

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
            ActionCommand.CanExecuteCommand = HasCredentials;
            CurrentCredentials = InstanceCredentials.FirstOrDefault();
        }

        private void OnActionCommand()
        {
            Result = CurrentCredentials;
            _owner.Close();
        }
    }
}
