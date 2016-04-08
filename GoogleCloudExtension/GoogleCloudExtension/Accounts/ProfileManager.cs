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
        internal static async Task<GPlusProfile> GetProfileForCredentialsAsync(UserAccount userCredentials)
        {
            var oauthToken = await AccountsManager.GetAccessTokenForCredentialsAsync(userCredentials);
            return await GPlusDataSource.GetProfileAsync(oauthToken);
        }
    }
}