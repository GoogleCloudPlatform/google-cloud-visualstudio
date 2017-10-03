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
        private bool _isEnabled = true;
        private string _errorMessage;

        /// <summary>
        /// Indicates if the git installation has been verified installed.
        /// </summary>
        public static bool GitInstallationVerified { get; private set; }

        /// <summary>
        /// Enable or disable the control.
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            private set { SetValueAndRaise(ref _isEnabled, value); }
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
            VerifyCommand = new ProtectedAsyncCommand(async () =>
            {
                if (await CheckInstallationAsync())
                {
                    _parent.OnGitInstallationCheckSuccess();
                }
            });
        }

        /// <summary>
        /// Check if Git for Windows dependency is installed properly.
        /// Set ErrorMessage so that the error shows 
        /// </summary>
        /// <returns>
        /// true: Verified git is installed.  false: git is not installed properly.
        /// </returns>
        public async Task<bool> CheckInstallationAsync()
        {
            if (GitInstallationVerified)
            {
                return true;
            }

            if (String.IsNullOrWhiteSpace(GitRepository.GetGitPath()))
            {
                ErrorMessage = Resources.GitUtilsMissingGitErrorTitle;
                return false;
            }

            IsEnabled = false;
            try
            {
                if (await GitRepository.IsGitCredentialManagerInstalledAsync())
                {
                    ErrorMessage = null;
                    GitInstallationVerified = true;
                }
                else
                {
                    ErrorMessage = Resources.GitUtilsGitCredentialManagerNotInstalledMessage;
                }
            }
            finally
            {
                IsEnabled = true;
            }

            return GitInstallationVerified;
        }
    }
}
