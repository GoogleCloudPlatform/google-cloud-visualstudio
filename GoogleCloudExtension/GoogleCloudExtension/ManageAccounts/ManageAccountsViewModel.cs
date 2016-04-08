using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.ManageAccounts
{
    public class ManageAccountsViewModel : ViewModelBase
    {
        private AsyncPropertyValue<IEnumerable<UserCredentialsViewModel>> _userCredentialsListAsync;
        private string _currentAccountName;

        public AsyncPropertyValue<IEnumerable<UserCredentialsViewModel>> UserCredentialsListAsync
        {
            get { return _userCredentialsListAsync; }
            set { SetValueAndRaise(ref _userCredentialsListAsync, value); }
        }

        public string CurrentAccountName
        {
            get { return _currentAccountName; }
            set { SetValueAndRaise(ref _currentAccountName, value); }
        }

        public ICommand CloseCommand { get; }

        public ICommand AddCredentialsCommand { get; }

        public ManageAccountsViewModel(ManageAccountsWindow owner)
        {
            _userCredentialsListAsync = new AsyncPropertyValue<IEnumerable<UserCredentialsViewModel>>(LoadUserCredentialsViewModel());

            CurrentAccountName = AccountsManager.CurrentCredentials?.AccountName;

            CloseCommand = new WeakCommand(owner.Close);
            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);

            AccountsManager.CurrentCredentialsChanged += OnCurrentCredentialsChanged;
        }

        private void OnCurrentCredentialsChanged(object sender, EventArgs e)
        {
            CurrentAccountName = AccountsManager.CurrentCredentials?.AccountName;
        }

        private void OnAddCredentialsCommand()
        {
            AccountsManager.LoginFlow();
        }

        private async Task<IEnumerable<UserCredentialsViewModel>> LoadUserCredentialsViewModel()
        {
            var userCredentials = await AccountsManager.GetCredentialsListAsync();
            var result = userCredentials.Select(x => new UserCredentialsViewModel(x)).ToList();
            return result;
        }
    }
}
