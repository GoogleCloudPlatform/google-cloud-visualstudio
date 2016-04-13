using GoogleCloudExtension.Accounts.Models;
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
    public class CredentialsStore
    {
        private const string AccountsStorePath = @"googlecloudvsextension\accounts";
        private const string CurrentAccountFileName = "current_account";

        private static readonly string s_credentialsStoreRoot = GetCredentialsStoreRoot();
        private static readonly string s_currentAccountMakerPath = GetCurrentAccountMarkerPath();

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
            _currentAccount = GetCurrentAccount();
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
        /// <param name="account"></param>
        /// <returns></returns>
        public void AddAccount(UserAccount account)
        {
            EnsureCredentialsRootExist();
            var serialized = JsonConvert.SerializeObject(account);
            var name = GetName(serialized);
            var savePath = Path.Combine(s_credentialsStoreRoot, name);
            Debug.WriteLine($"Saving account: {savePath}");
            File.WriteAllText(savePath, serialized);
            _cachedCredentials[account.AccountName] = new StoredUserAccount(
                fileName: name,
                userAccount: account);
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

        public string GetAccountCredentialsPath(string accountName)
        {
            StoredUserAccount result;
            if (_cachedCredentials.TryGetValue(accountName, out result))
            {
                return Path.Combine(s_credentialsStoreRoot, result.FileName);
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
                SetPersistedCurrentAccountName(userAccount.AccountName);
            }
        }

        private UserAccount GetCurrentAccount()
        {
            var accountName = GetPersistedCurrentAccountName();
            return accountName != null ? _cachedCredentials[accountName].UserAccount : null;
        }

        private string GetUserAccountPath(string accountName)
        {
            Debug.WriteLine($"Deleting account: {accountName}");
            StoredUserAccount storedAccount;
            if (!_cachedCredentials.TryGetValue(accountName, out storedAccount))
            {
                Debug.WriteLine($"Unknown account: {accountName}");
                return null;
            }

            return Path.Combine(s_credentialsStoreRoot, storedAccount.FileName);
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
                .Select(x => new StoredUserAccount(Path.GetFileName(x), LoadUserAccount(x)))
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

        private static string GetName(string serialized)
        {
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

        private static string GetPersistedCurrentAccountName()
        {
            if (!File.Exists(s_currentAccountMakerPath))
            {
                Debug.WriteLine($"Nothing to read, no current account marker exist: {s_currentAccountMakerPath}");
                return null;
            }

            Debug.WriteLine($"Reading current account name: {s_currentAccountMakerPath}");
            return File.ReadAllText(s_currentAccountMakerPath);
        }
      
        private static void SetPersistedCurrentAccountName(string accountName)
        {
            try
            {
                Debug.WriteLine($"Updating current account: {accountName} at {s_currentAccountMakerPath}");
                File.WriteAllText(s_currentAccountMakerPath, accountName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set marker: {ex.Message}");
            }
        }

        private static void DeleteCurrentAccountMarker()
        {
            if (!File.Exists(s_currentAccountMakerPath))
            {
                Debug.WriteLine($"Nothing to delete, current account marker does not exist: {s_currentAccountMakerPath}");
                return;
            }

            try
            {
                Debug.WriteLine("Deleting current account marker");
                File.Delete(s_currentAccountMakerPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete marker: {ex.Message}");
            }
        }
    }
}
