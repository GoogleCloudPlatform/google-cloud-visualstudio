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

using GoogleCloudExtension.GitUtils;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model to CsrGitSetupWarningContent.xaml
    /// </summary>
    public class CsrGitSetupWarningViewModel : ViewModelBase
    {
        private readonly CsrSectionControlViewModel _parent;
        private bool _isEnabled = false;
        private string _errorMessage;

        /// <summary>
        /// Indicates if the git installation has been verified installed.
        /// </summary>
        public static bool GitInstallationVerified { get; private set; }

        /// <summary>
        /// Enalbe or disable the control
        /// </summary>
        public bool IsEnable
        {
            get { return _isEnabled; }
            private set { SetValueAndRaise(ref _isEnabled, value);  }
        }

        /// <summary>
        /// The error message when git is not installed.
        /// </summary>
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetValueAndRaise(ref _errorMessage, value); }
        }

        /// <summary>
        /// Respond to InstallGit button
        /// </summary>
        public ICommand InstallGitCommand { get; }

        /// <summary>
        /// Respond to Test installation button
        /// </summary>
        public ICommand VerifyCommand { get; }

        public CsrGitSetupWarningViewModel(CsrSectionControlViewModel parent)
        {
            _parent = parent;
            InstallGitCommand = new ProtectedCommand(
                () => Process.Start(ValidateGitDependencyHelper.GitInstallationLink));
            VerifyCommand = new ProtectedAsyncCommand(VerifyInstallation);
        }

        /// <summary>
        /// Check if Git for Windows dependency is installed properly.
        /// Set s_error so that the error shows 
        /// </summary>
        public async Task CheckInstallation()
        {
            if (GitInstallationVerified)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(GitRepository.GetGitPath()))
            {
                ErrorMessage = Resources.GitUtilsMissingGitErrorTitle;
                return;
            }
            try
            {
                await GitRepository.GitCredentialManagerInstalled();
            }
            catch (GitCommandException)
            {
                ErrorMessage = Resources.GitUtilsGitCredentialManagerNotInstalledMessage;
                return;
            }

            ErrorMessage = null;
            GitInstallationVerified = true;
        }

        private async Task VerifyInstallation()
        {
            IsEnable = false;
            try
            {
                await CheckInstallation();
                if (GitInstallationVerified)
                {
                    _parent.OnGitInstallationCheckSuccess();
                }
            }
            finally
            {
                IsEnable = true;
            }
        }
    }
}
