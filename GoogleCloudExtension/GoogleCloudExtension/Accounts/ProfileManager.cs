using System;
using System.Windows.Media;
using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.Utils;
using System.Threading.Tasks;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.DataSources;

namespace GoogleCloudExtension.Accounts
{
    public static class ProfileManager
    {
        public static async Task<GPlusProfile> GetProfileForCredentialsAsync(UserAccount userAccount)
        {
            var oauthToken = await AccountsManager.GetAccessTokenForCredentialsAsync(userAccount);
            return await GPlusDataSource.GetProfileAsync(oauthToken);
        }
    }
}