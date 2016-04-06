using System;
using System.Windows.Media;
using GoogleCloudExtension.Credentials.Models;
using GoogleCloudExtension.Utils;
using System.Threading.Tasks;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.DataSources;

namespace GoogleCloudExtension.Credentials
{
    public static class ProfileManager
    {
        internal static async Task<GPlusProfile> GetProfileForCredentialsAsync(UserCredentials userCredentials)
        {
            var oauthToken = await CredentialsManager.GetAccessTokenForCredentialsAsync(userCredentials);
            return await GPlusDataSource.GetProfileAsync(oauthToken);
        }
    }
}