using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.OAuth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Accounts
{
    public static class AccountsManager
    {
        private const string CredentialsStorePath = @"googlecloudvsextension\accounts";

        private static readonly OAuthCredentials s_extensionCredentials =
            new OAuthCredentials(
                clientId: "622828670384-b6gc2gb8vfgvff80855u5oaubun5f6q2.apps.googleusercontent.com",
                clientSecret: "g-0P0bpUoO9n2NtocP25HRxm");
        private static readonly IEnumerable<string> s_extensionScopes =
            new List<string>
            {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/cloud-platform",
                "https://www.googleapis.com/auth/appengine.admin",
                "https://www.googleapis.com/auth/compute",
                "https://www.googleapis.com/auth/plus.me",
            };

        private static UserAccount s_currentCredentials;
        private static Lazy<string> s_userCredentialsPath = new Lazy<string>(GetCredentialsStorePath);
        private static Lazy<Task<IEnumerable<UserAccount>>> s_knownCredentials =
            new Lazy<Task<IEnumerable<UserAccount>>>(LoadKnownCredentialsAsync);

        public static UserAccount CurrentCredentials
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

        public static async void LoginFlow()
        {
            var url = OAuthManager.GetOAuthBeginFlowUrl(s_extensionCredentials, s_extensionScopes);
            string accessCode = await GetAcessCodeAsync(url);
            if (accessCode == null)
            {
                Debug.WriteLine("The user cancelled the OAUTH login flow.");
                return;
            }

            var loginResult = await OAuthManager.EndOAuthFlow(accessCode);
            var credentials = await GetCredentialsForLoginResultAsync(loginResult);

            await StoreUserCredentialsAsync(credentials);
            CurrentCredentials = credentials;
        }

        private static async Task<UserAccount> GetCredentialsForLoginResultAsync(OAuthLoginResult loginResult)
        {
            var profile = await GPlusDataSource.GetProfileAsync(loginResult.AccessToken.Token);
            return new UserAccount
            {
                AccountName = profile.Emails.FirstOrDefault()?.Value,
                RefreshToken = loginResult.RefreshToken,
            };
        }

        private static Task<string> GetAcessCodeAsync(string url)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the access token for the given <paramref name="userCredentials"/>.
        /// </summary>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        public static async Task<string> GetAccessTokenForCredentialsAsync(UserAccount userCredentials)
        {
            var accessToken = await OAuthManager.RefreshAccessTokenAsync(s_extensionCredentials, userCredentials.RefreshToken);
            return accessToken.Token;
        }

        /// <summary>
        /// Returns the list of credentials known to the extension.
        /// </summary>
        /// <returns></returns>
        public static Task<IEnumerable<UserAccount>> GetCredentialsListAsync() => s_knownCredentials.Value;

        /// <summary>
        /// Stores a new set of user credentials in the credentials store.
        /// </summary>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        public static async Task StoreUserCredentialsAsync(UserAccount userCredentials)
        {
            await Task.Run(() => userCredentials.Save(s_userCredentialsPath.Value));
        }

        private static string GetCredentialsStorePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, CredentialsStorePath);
        }

        private static Task<IEnumerable<UserAccount>> LoadKnownCredentialsAsync()
        {
            return Task.Run(() =>
                {
                    Debug.WriteLine($"Listing credentials in directory: {s_userCredentialsPath.Value}");
                    if (!Directory.Exists(s_userCredentialsPath.Value))
                    {
                        return Enumerable.Empty<UserAccount>();
                    }
                    return Directory.EnumerateFiles(s_userCredentialsPath.Value).Select(x => UserAccount.FromFile(x));
                });
        }
    }
}
