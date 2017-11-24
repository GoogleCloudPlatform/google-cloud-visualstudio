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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.ManageAccounts
{
    public class ManageAccountsViewModel : ViewModelBase
    {
        private readonly ManageAccountsWindow _owner;
        private IEnumerable<UserAccountViewModel> _userAccountsList;
        private UserAccountViewModel _currentUserAccount;
        private string _currentAccountName;

        public IEnumerable<UserAccountViewModel> UserAccountsList
        {
            get { return _userAccountsList; }
            set { SetValueAndRaise(ref _userAccountsList, value); }
        }

        public string CurrentAccountName
        {
            get { return _currentAccountName; }
            set { SetValueAndRaise(ref _currentAccountName, value); }
        }

        public UserAccountViewModel CurrentUserAccount
        {
            get { return _currentUserAccount; }
            set
            {
                SetValueAndRaise(ref _currentUserAccount, value);

                if (CredentialsStore.Default.CurrentAccount == null)
                {
                    SetAsCurrentAccountCommand.CanExecuteCommand = value != null;
                }
                else
                {
                    SetAsCurrentAccountCommand.CanExecuteCommand = value != null
                        && CredentialsStore.Default.CurrentAccount.AccountName != value.AccountName;
                }

                DeleteAccountCommand.CanExecuteCommand = value != null;
            }
        }

        public ProtectedCommand SetAsCurrentAccountCommand { get; }

        public ProtectedCommand DeleteAccountCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand AddAccountCommand { get; }

        public ManageAccountsViewModel(ManageAccountsWindow owner)
        {
            _owner = owner;
            _userAccountsList = LoadUserCredentialsViewModel();

            CurrentAccountName = CredentialsStore.Default.CurrentAccount?.AccountName;

            SetAsCurrentAccountCommand = new ProtectedCommand(OnSetAsCurrentAccountCommand, canExecuteCommand: false);
            DeleteAccountCommand = new ProtectedCommand(OnDeleteAccountCommand);
            CloseCommand = new ProtectedCommand(owner.Close);
            AddAccountCommand = new ProtectedCommand(OnAddAccountCommand);
        }

        public void DoucleClickedItem(UserAccountViewModel userAccount)
        {
            if (userAccount.IsCurrentAccount)
            {
                return;
            }

            CredentialsStore.Default.UpdateCurrentAccount(userAccount.UserAccount);
            _owner.Close();
        }

        private void OnDeleteAccountCommand()
        {
            Debug.WriteLine($"Attempting to delete account: {CurrentAccountName}");
            if (!UserPromptUtils.ActionPrompt(
                String.Format(Resources.ManageAccountsDeleteAccountPromptMessage, CurrentAccountName),
                Resources.ManageAccountsDeleteAccountPromptTitle,
                actionCaption: Resources.UiDeleteButtonCaption))
            {
                Debug.WriteLine($"The user cancelled the deletion of the account.");
                return;
            }

            AccountsManager.DeleteAccount(CurrentUserAccount.UserAccount);
            // Refreshing everything.
            UserAccountsList = LoadUserCredentialsViewModel();
        }

        private void OnSetAsCurrentAccountCommand()
        {
            Debug.WriteLine($"Setting current account: {CurrentAccountName}");
            CredentialsStore.Default.UpdateCurrentAccount(CurrentUserAccount.UserAccount);
            _owner.Close();
        }

        private async void OnAddAccountCommand()
        {
            Debug.WriteLine("Stating the oauth login flow.");
            if (await AccountsManager.StartAddAccountFlowAsync())
            {
                EventsReporterWrapper.ReportEvent(NewLoginEvent.Create());
                Debug.WriteLine($"The user logged in: {CredentialsStore.Default.CurrentAccount.AccountName}");
                _owner.Close();
            }
        }

        private IEnumerable<UserAccountViewModel> LoadUserCredentialsViewModel()
        {
            var userCredentials = CredentialsStore.Default.AccountsList;
            var result = userCredentials.Select(x => new UserAccountViewModel(x)).ToList();
            return result;
        }
    }
}
