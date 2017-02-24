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

using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ManageAccounts;
using System;

namespace GoogleCloudExtension.TitleBar
{
    /// <summary>
    /// View model to <seealso cref="TitleBar"/>.
    /// </summary>
    public class TitleBarViewModel : ViewModelBase
    {
        private bool _showAccountManagementLink;

        /// <summary>
        /// Gets current account name.
        /// </summary>
        public string AccountName => CredentialsStore.Default.CurrentAccount?.AccountName;

        /// <summary>
        /// Gets current project id.
        /// </summary>
        public string ProjectId => CredentialsStore.Default.CurrentProjectId;

        /// <summary>
        /// Gets the command for going to account management window.
        /// </summary>
        public ProtectedCommand OnGotoAccountManagementCommand { get; }

        /// <summary>
        /// Gets or sets Account Management link visibility.
        /// </summary>
        public bool ShowAccountManagementLink
        {
            get { return _showAccountManagementLink; }
            set { SetValueAndRaise(ref _showAccountManagementLink, value); }
        }

        /// <summary>
        /// Initializes a new instance of <seealso cref="TitleBarViewModel"/> class.
        /// Make it private to enforce the singleton pattern.
        /// </summary>
        public TitleBarViewModel()
        {
            OnGotoAccountManagementCommand = new ProtectedCommand(ManageAccountsWindow.PromptUser);
            CredentialsStore.Default.CurrentProjectIdChanged += (sender, e) => OnAccountProjectIdChanged();
            CredentialsStore.Default.Reset += (sender, e) => OnAccountProjectIdChanged();
        }

        private void OnAccountProjectIdChanged()
        {
            ShowAccountManagementLink = string.IsNullOrWhiteSpace(ProjectId);
            RaiseAllPropertyChanged();
        }
    }
}
