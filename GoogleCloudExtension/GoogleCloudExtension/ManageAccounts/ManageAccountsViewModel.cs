using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.ManageAccounts
{
    public class ManageAccountsViewModel : ViewModelBase
    {
        private readonly ManageAccountsWindow _owner;
        private IEnumerable<UserAccountViewModel> _userAccountsList;
        private UserAccountViewModel _currentUserAccount;
        private string _currentAccountName;
        private bool _currentAccountChanged;

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

                if (AccountsManager.CurrentAccount == null)
                {
                    ApplyCommand.CanExecuteCommand = value != null;
                }
                else
                {
                    ApplyCommand.CanExecuteCommand = value != null && AccountsManager.CurrentAccount.AccountName != value.AccountName;
                }
            }
        }

        public WeakCommand ApplyCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand AddCredentialsCommand { get; }

        public ManageAccountsViewModel(ManageAccountsWindow owner)
        {
            _owner = owner;
            _userAccountsList = LoadUserCredentialsViewModel();

            CurrentAccountName = AccountsManager.CurrentAccount?.AccountName;

            ApplyCommand = new WeakCommand(OnApplyCommand, canExecuteCommand: false);
            CloseCommand = new WeakCommand(owner.Close);
            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);
        }

        private void OnApplyCommand()
        {
            Debug.WriteLine($"Setting current account: {_currentAccountName}");
            AccountsManager.CurrentAccount = _currentUserAccount.UserAccount;
            _owner.Close();
        }

        private void OnAddCredentialsCommand()
        {
            Debug.WriteLine("Stating the oauth login flow.");
            AccountsManager.LoginFlow();
        }

        private IEnumerable<UserAccountViewModel> LoadUserCredentialsViewModel()
        {
            var userCredentials = AccountsManager.GetAccountsList();
            var result = userCredentials.Select(x => new UserAccountViewModel(x)).ToList();
            return result;
        }
    }
}
