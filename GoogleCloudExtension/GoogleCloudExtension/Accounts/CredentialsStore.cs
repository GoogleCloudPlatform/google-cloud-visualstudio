using GoogleCloudExtension.Accounts.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GoogleCloudExtension.Accounts
{
    public class CredentialsStore
    {
        private class StoredUserAccount
        {
            public string FileName { get; set; }

            public UserAccount UserAccount { get; set; }
        }

        private const string AccountsStorePath = @"googlecloudvsextension\accounts";
        private const string CurrentAccountFileName = "current_account";

        private static readonly string s_credentialsStoreRoot = GetCredentialsStoreRoot();
        private static readonly string s_currentAccountPath = GetCurrentAccountMarkerPath();

        private Dictionary<string, StoredUserAccount> _cachedCredentials;
        private UserAccount _currentAccount;

        public UserAccount CurrentAccount
        {
            get { return _currentAccount; }
            set
            {
                _currentAccount = value;
                SetCurrentAccount(value);
            }
        }

        public IEnumerable<UserAccount> AccountsList => _cachedCredentials.Values.Select(x => x.UserAccount);

        public CredentialsStore()
        {
            _cachedCredentials = LoadAccounts();
            _currentAccount = GetPersistedCurrentAccount();
        }

        /// <summary>
        /// Deletest the <paramref name="account"/> from the store.
        /// </summary>
        /// <param name="account">The accound to delete.</param>
        /// <returns>True if the current account was deleted, false otherwise.</returns>
        public bool DeleteAccount(UserAccount account)
        {
            var accountFilePath = GetUserAccountPath(account.AccountName);
            if (accountFilePath == null)
            {
                Debug.WriteLine($"Should not be here, unkonwn account anme: {account.AccountName}");
                throw new InvalidOperationException($"Unknown accout name: {account.AccountName}");
            }

            File.Delete(accountFilePath);
            bool result = false;

            if (account.AccountName == CurrentAccount?.AccountName)
            {
                DeleteCurrentAccountMarker();
                result = true;
            }

            _cachedCredentials = LoadAccounts();
            return result;
        }

        /// <summary>
        /// Stores a new set of user credentials in the credentials store.
        /// </summary>
        /// <param name="userAccount"></param>
        /// <returns></returns>
        public void AddAccount(UserAccount userAccount)
        {
            EnsureCredentialsRootExist();
            var name = SaveUserAccount(userAccount);
            _cachedCredentials[userAccount.AccountName] = new StoredUserAccount
            {
                FileName = name,
                UserAccount = userAccount,
            };
        }

        /// <summary>
        /// Returns the account given the account name.
        /// </summary>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public UserAccount GetAccount(string accountName)
        {
            StoredUserAccount result;
            if (_cachedCredentials.TryGetValue(accountName, out result))
            {
                return result.UserAccount;
            }
            return null;
        }

        public string GetDefaultCurrentAccountPath() => s_currentAccountPath;

        private string GetUserAccountPath(string accountName)
        {
            StoredUserAccount stored;
            if (_cachedCredentials.TryGetValue(accountName, out stored))
            {
                return Path.Combine(s_credentialsStoreRoot, stored.FileName);
            }
            return null;
        }

        private void SetCurrentAccount(UserAccount userAccount)
        {
            if (userAccount == null)
            {
                DeleteCurrentAccountMarker();
            }
            else
            {
                SetPersistedCurrentAccount(userAccount);
            }
        }

        private static Dictionary<string, StoredUserAccount> LoadAccounts()
        {
            Debug.WriteLine($"Listing credentials in directory: {s_credentialsStoreRoot}");
            if (!Directory.Exists(s_credentialsStoreRoot))
            {
                return new Dictionary<string, StoredUserAccount>();
            }
            return Directory.EnumerateFiles(s_credentialsStoreRoot)
                .Where(x => Path.GetExtension(x) == ".json")
                .Select(x => new StoredUserAccount { FileName = Path.GetFileName(x), UserAccount = LoadUserAccount(x) })
                .ToDictionary(x => x.UserAccount.AccountName, y => y);
        }

        private static void EnsureCredentialsRootExist()
        {
            if (Directory.Exists(s_credentialsStoreRoot))
            {
                return;
            }
            Debug.WriteLine($"Creating directory {s_credentialsStoreRoot}");
            Directory.CreateDirectory(s_credentialsStoreRoot);
        }

        private static string GetCredentialsStoreRoot()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, AccountsStorePath);
        }

        private static UserAccount LoadUserAccount(string path)
        {
            var contents = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UserAccount>(contents);
        }

        private static void SaveUserAccount(UserAccount userAccount, string path)
        {
            try
            {
                var serialized = JsonConvert.SerializeObject(userAccount);
                File.WriteAllText(path, serialized);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save user account to {path}: {ex.Message}");
            }
        }

        private static string SaveUserAccount(UserAccount userAccount)
        {
            var name = GetName(userAccount);
            var savePath = Path.Combine(s_credentialsStoreRoot, name);
            SaveUserAccount(userAccount, savePath);
            return name;
        }

        private static string GetName(UserAccount userAccount)
        {
            var serialized = JsonConvert.SerializeObject(userAccount);
            var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(serialized));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            sb.Append(".json");
            return sb.ToString();
        }

        private static string GetCurrentAccountMarkerPath()
        {
            return Path.Combine(s_credentialsStoreRoot, CurrentAccountFileName);
        }

        private static UserAccount GetPersistedCurrentAccount()
        {
            if (!File.Exists(s_currentAccountPath))
            {
                Debug.WriteLine($"Nothing to read, no current account exist: {s_currentAccountPath}");
                return null;
            }

            Debug.WriteLine($"Reading current account: {s_currentAccountPath}");
            return LoadUserAccount(s_currentAccountPath);
        }
      
        private static void SetPersistedCurrentAccount(UserAccount userAccount)
        {
            try
            {
                Debug.WriteLine($"Updating current account: {userAccount.AccountName} at {s_currentAccountPath}");
                SaveUserAccount(userAccount, s_currentAccountPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set marker: {ex.Message}");
            }
        }

        private static void DeleteCurrentAccountMarker()
        {
            if (!File.Exists(s_currentAccountPath))
            {
                Debug.WriteLine($"Nothing to delete, current account marker does not exist: {s_currentAccountPath}");
                return;
            }

            try
            {
                Debug.WriteLine("Deleting current account marker");
                File.Delete(s_currentAccountPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete marker: {ex.Message}");
            }
        }
    }
}
