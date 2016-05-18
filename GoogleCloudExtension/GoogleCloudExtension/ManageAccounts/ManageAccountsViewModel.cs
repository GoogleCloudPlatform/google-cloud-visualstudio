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
using GoogleCloudExtension.Utils;
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
                    SetAsCurrentAcountCommand.CanExecuteCommand = value != null;
                }
                else
                {
                    SetAsCurrentAcountCommand.CanExecuteCommand = value != null
                        && CredentialsStore.Default.CurrentAccount.AccountName != value.AccountName;
                }

                DeleteAccountCommand.CanExecuteCommand = value != null;
            }
        }

        public WeakCommand SetAsCurrentAcountCommand { get; }

        public WeakCommand DeleteAccountCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand AddCredentialsCommand { get; }

        public ManageAccountsViewModel(ManageAccountsWindow owner)
        {
            _owner = owner;
            _userAccountsList = LoadUserCredentialsViewModel();

            CurrentAccountName = CredentialsStore.Default.CurrentAccount?.AccountName;

            SetAsCurrentAcountCommand = new WeakCommand(OnSetAsCurrentAccountCommand, canExecuteCommand: false);
            DeleteAccountCommand = new WeakCommand(OnDeleteAccountCommand);
            CloseCommand = new WeakCommand(owner.Close);
            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);
        }

        public void DoucleClickedItem(UserAccountViewModel userAccount)
        {
            ExtensionAnalytics.ReportCommand(CommandName.DoubleClickedAccountCommand, CommandInvocationSource.ListItem);

            if (userAccount.IsCurrentAccount)
            {
                return;
            }

            CredentialsStore.Default.CurrentAccount = userAccount.UserAccount;
            _owner.Close();
        }

        private void OnDeleteAccountCommand()
        {
            ExtensionAnalytics.ReportCommand(CommandName.DeleteAccountCommand, CommandInvocationSource.Button);

            Debug.WriteLine($"Attempting to delete account: {CurrentAccountName}");
            if (!UserPromptUtils.YesNoPrompt($"Are you sure you want to delete the account {CurrentAccountName}", "Delete Account"))
            {
                ExtensionAnalytics.ReportEvent("DeleteAccountCommandCancelled", "Cancelled");
                Debug.WriteLine($"The user cancelled the deletion of the account.");
                return;
            }

            AccountsManager.DeleteAccount(CurrentUserAccount.UserAccount);
            // Refreshing everything.
            UserAccountsList = LoadUserCredentialsViewModel();
        }

        private void OnSetAsCurrentAccountCommand()
        {
            ExtensionAnalytics.ReportCommand(CommandName.SetCurrentAccountCommand, CommandInvocationSource.Button);

            Debug.WriteLine($"Setting current account: {CurrentAccountName}");
            CredentialsStore.Default.CurrentAccount = CurrentUserAccount.UserAccount;
            _owner.Close();
        }

        private async void OnAddCredentialsCommand()
        {
            ExtensionAnalytics.ReportCommand(CommandName.AddAccountCommand, CommandInvocationSource.Button);

            Debug.WriteLine("Stating the oauth login flow.");
            if (await AccountsManager.StartAddAccountFlowAsync())
            {
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
