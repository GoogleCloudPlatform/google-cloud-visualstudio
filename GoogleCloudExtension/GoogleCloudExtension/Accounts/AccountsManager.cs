using Google.Apis.Auth.OAuth2;
using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.OauthLoginFlow;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Accounts
{
    public static class AccountsManager
    {
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

        private static CredentialsStore s_credentialsStore = new CredentialsStore();

        public static UserAccount CurrentAccount
        {
            get { return s_credentialsStore.CurrentAccount; }
            set
            {
                s_credentialsStore.CurrentAccount = value;
                CurrentCredentialsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static GoogleCredential CurrentGoogleCredential => CurrentAccount?.GetGoogleCredential();

        public static event EventHandler CurrentCredentialsChanged;

        static AccountsManager()
        {
            SetCurrentCredentialsEnvironmentVariable();
        }

        public static async Task<bool> AddAccountFlowAsync()
        {
            var url = OAuthManager.GetOAuthBeginFlowUrl(s_extensionCredentials, s_extensionScopes);
            string accessCode = OAuthLoginFlowWindow.RunOAuthFlow(url);
            if (accessCode == null)
            {
                Debug.WriteLine("The user cancelled the OAUTH login flow.");
                return false;
            }

            var loginResult = await OAuthManager.EndOAuthFlow(s_extensionCredentials, accessCode);
            var credentials = await GetUserAccountForLoginResult(loginResult);

            var existingUserAccount = s_credentialsStore.GetAccount(credentials.AccountName);
            if (existingUserAccount != null)
            {
                Debug.WriteLine($"Duplicated account {credentials.AccountName}");
                UserPromptUtils.OkPrompt($"The user account {credentials.AccountName} already exists.", "Duplicate Account");
                return false;
            }

            s_credentialsStore.AddAccount(credentials);
            return true;
        }

        public static void DeleteAccount(UserAccount userAccount)
        {
            var deletedCurrentAccount = s_credentialsStore.DeleteAccount(userAccount);
            if (deletedCurrentAccount)
            {
                CurrentCredentialsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static IEnumerable<UserAccount> GetAccountsList() => s_credentialsStore.AccountsList;

        private static async Task<UserAccount> GetUserAccountForLoginResult(OAuthLoginResult loginResult)
        {
            var result = new UserAccount
            {
                RefreshToken = loginResult.RefreshToken,
                ClientId = s_extensionCredentials.ClientId,
                ClientSecret = s_extensionCredentials.ClientSecret
            };
            var plusDataSource = new GPlusDataSource(result.GetGoogleCredential());
            var person = await plusDataSource.GetProfileAsync();
            result.AccountName = person.Emails.FirstOrDefault()?.Value;
            return result;
        }

        private static void SetCurrentCredentialsEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                s_credentialsStore.GetDefaultCurrentAccountPath());
        }
    }
}
