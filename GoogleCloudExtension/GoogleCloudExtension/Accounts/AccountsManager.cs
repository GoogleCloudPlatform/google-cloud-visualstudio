using GoogleCloudExtension.Accounts.Models;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.OAuth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Accounts
{
    public static class AccountsManager
    {
        private const string AccountsStorePath = @"googlecloudvsextension\accounts";
        private const string CurrentAccountFileName = "current_account";

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

        private static UserAccount s_currentAccount;
        private static readonly string s_userCredentialsPath;
        private static readonly Dictionary<string, StoredUserAccount> s_accounts;

        static AccountsManager()
        {
            s_userCredentialsPath = GetCredentialsStorePath();
            s_accounts = LoadAccounts();

            var currentAccountFileName = GetCurrentAccountFileName();
            if (currentAccountFileName != null)
            {
                s_currentAccount = s_accounts.Values.FirstOrDefault(x => x.FileName == currentAccountFileName)?.UserAccount;
                if (s_currentAccount != null)
                {
                    Debug.WriteLine($"Current account found: {s_currentAccount.AccountName}");
                }
                else
                {
                    Debug.WriteLine("No current account found.");
                }
            }
        }

        public static UserAccount CurrentAccount
        {
            get { return s_currentAccount; }
            set
            {
                s_currentAccount = value;
                UpdateCurrentAccount(s_currentAccount);
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
            if (CurrentAccount != null)
            {
                return GetAccessTokenForCredentialsAsync(CurrentAccount);
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
            CurrentAccount = credentials;
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
        public static IEnumerable<UserAccount> GetAccountsList()
        {
            return s_accounts.Values.Select(x => x.UserAccount);
        }

        /// <summary>
        /// Stores a new set of user credentials in the credentials store.
        /// </summary>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        public static async Task StoreUserCredentialsAsync(UserAccount userCredentials)
        {
            await Task.Run(() => SaveUserAccount(userCredentials, s_userCredentialsPath));
        }

        private static string GetCredentialsStorePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, AccountsStorePath);
        }

        private static Dictionary<string, StoredUserAccount> LoadAccounts()
        {
            Debug.WriteLine($"Listing credentials in directory: {s_userCredentialsPath}");
            if (!Directory.Exists(s_userCredentialsPath))
            {
                return new Dictionary<string, StoredUserAccount>();
            }
            return Directory.EnumerateFiles(s_userCredentialsPath)
                .Where(x => Path.GetExtension(x) == ".json")
                .Select(x => new StoredUserAccount(Path.GetFileName(x), LoadUserAccount(x)))
                .ToDictionary(x => x.UserAccount.AccountName, y => y);
        }

        private static UserAccount LoadUserAccount(string path)
        {
            var contents = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UserAccount>(contents);
        }

        internal static string SaveUserAccount(UserAccount userAccount, string path)
        {
            var serialized = JsonConvert.SerializeObject(userAccount);
            var name = GetName(serialized);
            var savePath = Path.Combine(path, name);
            Debug.WriteLine($"Saving account: {savePath}");
            File.WriteAllText(savePath, serialized);
            return name;
        }

        /// <summary>
        /// Generates a unique name for the user credentials based on the hash of the contents
        /// which guarantees a safe and unique name for the credentials file.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        private static string GetName(string serialized)
        {
            var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(serialized));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.AppendFormat("{0:2x}", b);
            }
            sb.Append(".json");
            return sb.ToString();
        }

        private static string GetName(UserAccount userAccount)
        {
            return GetName(JsonConvert.SerializeObject(userAccount));
        }

        private static string GetCurrentAccountMarkerPath()
        {
            return Path.Combine(s_userCredentialsPath, CurrentAccountFileName);
        }

        private static string GetCurrentAccountFileName()
        {
            string currentAccountMarkerPath = GetCurrentAccountMarkerPath();
            if (!File.Exists(currentAccountMarkerPath))
            {
                return null;
            }

            Debug.WriteLine($"Reading current account name: {currentAccountMarkerPath}");
            return File.ReadAllText(currentAccountMarkerPath);
        }

        private static void SetCurrentAccountFileName(string fileName)
        {
            var currentAccountMarkerPath = GetCurrentAccountMarkerPath();

            Debug.WriteLine($"Updating current account: {currentAccountMarkerPath}");
            File.WriteAllText(currentAccountMarkerPath, fileName);
        }

        private static void UpdateCurrentAccount(UserAccount userAccount)
        {
            var storedUserAccount = s_accounts[userAccount.AccountName];
            SetCurrentAccountFileName(storedUserAccount.FileName);
        }
    }
}
