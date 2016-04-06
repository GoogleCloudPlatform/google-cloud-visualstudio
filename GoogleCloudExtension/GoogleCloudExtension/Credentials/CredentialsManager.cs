using GoogleCloudExtension.Credentials.Models;
using GoogleCloudExtension.OAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Credentials
{
    public static class CredentialsManager
    {
        private const string CredentialsStorePath = @"googlecloudvsextension\credentials";

        private static readonly OAuthCredentials s_extensionCredentials =
            new OAuthCredentials(
                clientId: "622828670384-b6gc2gb8vfgvff80855u5oaubun5f6q2.apps.googleusercontent.com",
                clientSecret: "g-0P0bpUoO9n2NtocP25HRxm");

        private static UserCredentials s_currentCredentials;
        private static Lazy<string> s_userCredentialsPath = new Lazy<string>(GetCredentialsStorePath);
        private static Lazy<Task<IEnumerable<UserCredentials>>> s_knownCredentials =
            new Lazy<Task<IEnumerable<UserCredentials>>>(LoadKnownCredentialsAsync);

        public static UserCredentials CurrentCredentials
        {
            get { return s_currentCredentials; }
            set
            {
                s_currentCredentials = value;
                CurrentCredentialsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler CurrentCredentialsChanged;

        /// <summary>
        /// Returns the access token to use for the current user.
        /// </summary>
        /// <returns></returns>
        public static Task<string> GetAccessTokenAsync()
        {
            if (CurrentCredentials != null)
            {
                return GetAccessTokenForCredentialsAsync(CurrentCredentials);
            }
            throw new InvalidOperationException("No current credential is set.");
        }

        /// <summary>
        /// Returns the access token for the given <paramref name="userCredentials"/>.
        /// </summary>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        public static Task<string> GetAccessTokenForCredentialsAsync(UserCredentials userCredentials)
        {
            return OAuthManager.RefreshAccessTokenAsync(s_extensionCredentials, userCredentials.RefreshToken);
        }

        /// <summary>
        /// Returns the list of credentials known to the extension.
        /// </summary>
        /// <returns></returns>
        public static Task<IEnumerable<UserCredentials>> GetCredentialsListAsync() => s_knownCredentials.Value;

        /// <summary>
        /// Stores a new set of user credentials in the credentials store.
        /// </summary>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        public static async Task StoreUserCredentialsAsync(UserCredentials userCredentials)
        {
            await Task.Run(() => userCredentials.SaveToStore(s_userCredentialsPath.Value));
        }

        private static string GetCredentialsStorePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, CredentialsStorePath);
        }

        private static Task<IEnumerable<UserCredentials>> LoadKnownCredentialsAsync()
        {
            return Task.Run(() =>
                {
                    return Directory.EnumerateFiles(s_userCredentialsPath.Value).Select(x => UserCredentials.FromFile(x));
                });
        }
    }
}
