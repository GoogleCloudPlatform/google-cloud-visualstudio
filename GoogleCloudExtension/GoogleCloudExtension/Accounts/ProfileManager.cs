using System;
using System.Windows.Media;
using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.Utils;
using System.Threading.Tasks;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.DataSources;
using Google.Apis.Plus.v1.Data;

namespace GoogleCloudExtension.Accounts
{
    public static class ProfileManager
    {
        public static async Task<Person> GetProfileForCredentialsAsync(UserAccount userAccount)
        {
            var dataSource = new GPlusDataSource(AccountsManager.GetGoogleCredential(userAccount));
            return await dataSource.GetProfileAsync();
        }
    }
}