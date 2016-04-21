using GoogleCloudExtension.Accounts;
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

                if (AccountsManager.CurrentAccount == null)
                {
                    SetAsCurrentAcountCommand.CanExecuteCommand = value != null;
                }
                else
                {
                    SetAsCurrentAcountCommand.CanExecuteCommand = value != null && AccountsManager.CurrentAccount.AccountName != value.AccountName;
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

            CurrentAccountName = AccountsManager.CurrentAccount?.AccountName;

            SetAsCurrentAcountCommand = new WeakCommand(OnSetAsCurrentAccountCommand, canExecuteCommand: false);
            DeleteAccountCommand = new WeakCommand(OnDeleteAccountCommand);
            CloseCommand = new WeakCommand(owner.Close);
            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);
        }

        public void DoucleClickedItem(UserAccountViewModel userAccount)
        {
            if (userAccount.IsCurrentAccount)
            {
                return;
            }

            AccountsManager.CurrentAccount = userAccount.UserAccount;
            _owner.Close();
        }

        private void OnDeleteAccountCommand()
        {
            Debug.WriteLine($"Attempting to delete account: {CurrentAccountName}");
            if (!UserPromptUtils.YesNoPrompt($"Are you sure you want to delete the account {CurrentAccountName}", "Delete Account"))
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
            AccountsManager.CurrentAccount = CurrentUserAccount.UserAccount;
            _owner.Close();
        }

        private async void OnAddCredentialsCommand()
        {
            Debug.WriteLine("Stating the oauth login flow.");
            if (await AccountsManager.AddAccountFlowAsync())
            {
                Debug.WriteLine("The user logged in, refresh the list of accounts.");
                UserAccountsList = LoadUserCredentialsViewModel();
            }
        }

        private IEnumerable<UserAccountViewModel> LoadUserCredentialsViewModel()
        {
            var userCredentials = AccountsManager.GetAccountsList();
            var result = userCredentials.Select(x => new UserAccountViewModel(x)).ToList();
            return result;
        }
    }
}
