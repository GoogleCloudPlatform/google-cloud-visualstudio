using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.ManageAccounts
{
    public class UserCredentialsViewModel : Model
    {
        private readonly UserAccount _userAccount;

        public AsyncPropertyValue<string> ProfilePictureAsync { get; }

        public AsyncPropertyValue<string> NameAsync { get; }

        public string AccountName { get; }

        public bool IsCurrentAccount => AccountsManager.CurrentCredentials?.AccountName == _userAccount.AccountName;

        public WeakCommand SetAsCurrentCommand { get; }

        public ICommand DeleteCommand { get; }

        public UserCredentialsViewModel(UserAccount userAccount)
        {
            _userAccount = userAccount;

            AccountName = userAccount.AccountName;

            var profileTask = ProfileManager.GetProfileForCredentialsAsync(userAccount);

            // TODO: Show the default image while it is being loaded.
            ProfilePictureAsync = AsyncPropertyValue<string>.CreateAsyncProperty(profileTask, x => x.Image.Url);
            NameAsync = AsyncPropertyValue<string>.CreateAsyncProperty(profileTask, x => x.DisplayName);

            // Commands.
            SetAsCurrentCommand = new WeakCommand(OnSetAsCurrentCommand, !IsCurrentAccount);
            DeleteCommand = new WeakCommand(OnDeleteCommand);

            // Be notified of changes in current account.
            AccountsManager.CurrentCredentialsChanged += OnCurrentCredentialsChanged;
        }

        private void OnCurrentCredentialsChanged(object sender, EventArgs e)
        {
            SetAsCurrentCommand.CanExecuteCommand = !IsCurrentAccount;
            RaisePropertyChanged(nameof(IsCurrentAccount));
        }

        private void OnDeleteCommand()
        {
            throw new NotImplementedException();
        }

        private void OnSetAsCurrentCommand()
        {
            AccountsManager.CurrentCredentials = _userAccount;
        }
    }
}
