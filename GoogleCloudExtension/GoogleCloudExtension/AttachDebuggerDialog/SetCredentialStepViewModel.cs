// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.PowerShellUtils;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Ask user to choose an account 
    /// </summary>
    public class SetCredentialStepViewModel : AttachDebuggerStepBase
    {
        private List<WindowsInstanceCredentials> _credentials;
        private WindowsInstanceCredentials _selectedCredentials;
        private bool _showSelection;

        /// <summary>
        /// Set to show the selection UI section.
        /// </summary>
        public bool ShowSelection
        {
            get { return _showSelection; }
            private set { SetValueAndRaise(ref _showSelection, value); }
        }

        /// <summary>
        /// The list of credentials available for the selected <seealso cref="Google.Apis.Compute.v1.Data.Instance"/>.
        /// </summary>
        public List<WindowsInstanceCredentials> Credentials
        {
            get { return _credentials; }
            private set { SetValueAndRaise(ref _credentials, value); }
        }

        /// <summary>
        /// The selected <seealso cref="WindowsInstanceCredentials"/> to use for the publish process.
        /// </summary>
        public WindowsInstanceCredentials SelectedCredentials
        {
            get { return _selectedCredentials; }
            set{ SetValueAndRaise(ref _selectedCredentials, value); }
        }

        /// <summary>
        /// The command to execute when pressing the manage credentials button.
        /// </summary>
        public ProtectedCommand ManageCredentialsCommand { get; }

        public SetCredentialStepViewModel(SetCredentialStepContent content, AttachDebuggerContext context)
            : base(context)
        {
            Content = content;
            ManageCredentialsCommand = new ProtectedCommand(() =>
            {
                ManageWindowsCredentialsWindow.PromptUser(context.GceInstance);
                UpdateCredentials();
                IsOKButtonEnabled = Credentials.Count() > 0;
            });
        }

        #region Implement interface IAttachDebuggerStep
        public override ContentControl Content { get; }

        public override Task<IAttachDebuggerStep> OnStartAsync()
        {
            IsCancelButtonEnabled = false;

            string user = AttachDebuggerSettings.Current.GetInstanceDefaultUser(Context.GceInstance);
            if (user != null)
            {
                Credentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(Context.GceInstance).ToList();
                WindowsInstanceCredentials credential = Credentials.FirstOrDefault(x => x.User == user);
                if (credential != null)
                {
                    Context.Password = PsUtils.ConvertToSecureString(credential.Password);
                    Context.Username = credential.User;

                    return Task.FromResult(DefaultNextStep);
                }
            }

            UpdateCredentials();
            if (Credentials.Count() == 1)
            {
                // Pick first user as default.
                SetDefaultCredential();
                return Task.FromResult(DefaultNextStep);
            }

            ShowSelection = true;
            IsCancelButtonEnabled = true;
            IsOKButtonEnabled = Credentials.Count() > 0;
            return Task.FromResult<IAttachDebuggerStep>(null);
        }

        public override Task<IAttachDebuggerStep> OnOkCommandAsync()
        {
            SetDefaultCredential();
            return Task.FromResult(DefaultNextStep);
        }
        #endregion

        /// <summary>
        /// Create the step that set credential
        /// </summary>
        public static SetCredentialStepViewModel CreateStep(AttachDebuggerContext context)
        {
            var content = new SetCredentialStepContent();
            var step = new SetCredentialStepViewModel(content, context);
            content.DataContext = step;
            return step;
        }

        private IAttachDebuggerStep DefaultNextStep => EnableDebuggerPortStepViewModel.CreateStep(Context);

        private void SetDefaultCredential()
        {
            AttachDebuggerSettings.Current.SetInstanceDefaultUser(Context.GceInstance, SelectedCredentials.User);
            Context.Password = PsUtils.ConvertToSecureString(SelectedCredentials.Password);
            Context.Username = SelectedCredentials.User;
        }

        private void UpdateCredentials()
        {
            Credentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(Context.GceInstance).ToList();
            SelectedCredentials = Credentials.FirstOrDefault();
        }
    }
}
