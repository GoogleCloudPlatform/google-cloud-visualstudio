using GoogleCloudExtension.Credentials;
using GoogleCloudExtension.Credentials.Models;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.ManageCredentials
{
    public class ManageCredentialsViewModel : ViewModelBase
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

        public ManageCredentialsViewModel(ManageCredentialsWindow owner)
        {
            _userCredentialsListAsync = new AsyncPropertyValue<IEnumerable<UserCredentialsViewModel>>(LoadUserCredentialsViewModel());

            CurrentAccountName = CredentialsManager.CurrentCredentials?.AccountName;

            CloseCommand = new WeakCommand(owner.Close);
            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);

            CredentialsManager.CurrentCredentialsChanged += OnCurrentCredentialsChanged;
        }

        private void OnCurrentCredentialsChanged(object sender, EventArgs e)
        {
            CurrentAccountName = CredentialsManager.CurrentCredentials?.AccountName;
        }

        private void OnAddCredentialsCommand()
        {
            CredentialsManager.LoginFlow();
        }

        private async Task<IEnumerable<UserCredentialsViewModel>> LoadUserCredentialsViewModel()
        {
            var userCredentials = await CredentialsManager.GetCredentialsListAsync();
            var result = userCredentials.Select(x => new UserCredentialsViewModel(x)).ToList();
            return result;
        }
    }
}
