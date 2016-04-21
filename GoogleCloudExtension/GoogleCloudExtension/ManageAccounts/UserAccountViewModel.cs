using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;

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

            var dataSource = new GPlusDataSource(userAccount.GetGoogleCredential());
            var personTask = dataSource.GetProfileAsync();

            // TODO: Show the default image while it is being loaded.
            ProfilePictureAsync = AsyncPropertyValue<string>.CreateAsyncProperty(personTask, x => x.Image.Url);
            NameAsync = AsyncPropertyValue<string>.CreateAsyncProperty(personTask, x => x.DisplayName, "Loading...");
        }
    }
}
