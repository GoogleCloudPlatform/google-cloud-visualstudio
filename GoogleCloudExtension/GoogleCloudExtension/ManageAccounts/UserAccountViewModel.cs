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
    public class UserAccountViewModel : Model
    {
        public AsyncPropertyValue<string> ProfilePictureAsync { get; }

        public AsyncPropertyValue<string> NameAsync { get; }

        public string AccountName { get; }

        public UserAccount UserAccount { get; }

        public bool IsCurrentAccount => AccountsManager.CurrentAccount?.AccountName == UserAccount.AccountName;

        public UserAccountViewModel(UserAccount userAccount)
        {
            UserAccount = userAccount;

            AccountName = userAccount.AccountName;

            var profileTask = ProfileManager.GetProfileForCredentialsAsync(userAccount);

            // TODO: Show the default image while it is being loaded.
            ProfilePictureAsync = AsyncPropertyValue<string>.CreateAsyncProperty(profileTask, x => x.Image.Url);
            NameAsync = AsyncPropertyValue<string>.CreateAsyncProperty(profileTask, x => x.DisplayName, "Loading...");
        }
    }
}
