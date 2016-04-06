using System;
using System.Windows.Media;
using GoogleCloudExtension.Credentials.Models;
using GoogleCloudExtension.Utils;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Credentials
{
    public static class ProfileManager
    {
        internal static Task<ImageSource> GetProfilePictureForCredentialsAsync(UserCredentials userCredentials)
        {
            throw new NotImplementedException();
        }

        internal static Task<string> GetProfileNameForCredentialsAsync(UserCredentials userCredentials)
        {
            throw new NotImplementedException();
        }
    }
}