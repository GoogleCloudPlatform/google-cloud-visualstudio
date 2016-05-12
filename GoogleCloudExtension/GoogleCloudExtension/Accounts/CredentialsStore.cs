// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Auth.OAuth2;
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
    /// <summary>
    /// This class manages the credentials store for the extension. Loading and saving the <see cref="UserAccount"/>
    /// to the %HOME%\AppData\Local directory. This class also manages the default credentials to use when a project
    /// doesn't specify credentials, or when Visual Studio is freshly opened. This default credentials is the last
    /// set of credentials used by the extension.
    /// </summary>
    public class CredentialsStore
    {
        /// <summary>
        /// Remembers the file name used to serialize a particular <see cref="UserAccount"/>.
        /// </summary>
        private class StoredUserAccount
        {
            public string FileName { get; set; }

            public UserAccount UserAccount { get; set; }
        }

        private const string AccountsStorePath = @"googlecloudvsextension\accounts";
        private const string DefaultCredentialsFileName = "default_credentials";

        private static readonly string s_credentialsStoreRoot = GetCredentialsStoreRoot();
        private static readonly Lazy<CredentialsStore> s_defaultCredentialsStore = new Lazy<CredentialsStore>(() => new CredentialsStore());

        private Dictionary<string, StoredUserAccount> _cachedCredentials;
        private UserAccount _currentAccount;
        private string _currentProjectId;

        public static CredentialsStore Default => s_defaultCredentialsStore.Value;

        public event EventHandler CurrentAccountChanged;
        public event EventHandler CurrentProjectIdChanged;
        public event EventHandler Reset;

        /// <summary>
        /// The current <see cref="UserAccount"/> selected.S
        /// </summary>
        public UserAccount CurrentAccount
        {
            get { return _currentAccount; }
            set
            {
                if (_currentAccount?.AccountName != value?.AccountName)
                {
                    _currentAccount = value;
                    UpdateDefaultAccountProjectId();
                    CurrentAccountChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// The GoogleCredential for the current <see cref="UserAccount"/>.
        /// </summary>
        public GoogleCredential CurrentGoogleCredential => CurrentAccount?.GetGoogleCredential();

        /// <summary>
        /// The currently selected project ID.
        /// </summary>
        public string CurrentProjectId
        {
            get { return _currentProjectId; }
            set
            {
                if (_currentProjectId != value)
                {
                    _currentProjectId = value;
                    UpdateDefaultAccountProjectId();
                    CurrentProjectIdChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// The list of accounts known to the store.
        /// </summary>
        public IEnumerable<UserAccount> AccountsList => _cachedCredentials.Values.Select(x => x.UserAccount);

        private CredentialsStore()
        {
            _cachedCredentials = LoadAccounts();

            var defaultCredentials = LoadDefaultCredentials();
            if (defaultCredentials != null)
            {
                ResetCredentials(defaultCredentials.AccountName, defaultCredentials.ProjectId);
            }
        }

        /// <summary>
        /// Deletes the <paramref name="account"/> from the store. The account must exist in the store
        /// or it will throw.
        /// </summary>
        /// <param name="account">The accound to delete.</param>
        /// <returns>True if the current account was deleted, false otherwise.</returns>
        public void DeleteAccount(UserAccount account)
        {
            var accountFilePath = GetUserAccountPath(account.AccountName);
            if (accountFilePath == null)
            {
                Debug.WriteLine($"Should not be here, unkonwn account anme: {account.AccountName}");
                throw new InvalidOperationException($"Unknown accout name: {account.AccountName}");
            }

            File.Delete(accountFilePath);
            var isCurrentAccount = account.AccountName == CurrentAccount?.AccountName;
            _cachedCredentials = LoadAccounts();
            if (isCurrentAccount)
            {
                ResetCredentials(null, null);
            }
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
        /// <param name="accountName">The name to look.</param>
        /// <returns>The account if found, null otherwise.</returns>
        public UserAccount GetAccount(string accountName)
        {
            if (accountName == null)
            {
                return null;
            }

            StoredUserAccount result;
            if (_cachedCredentials.TryGetValue(accountName, out result))
            {
                return result.UserAccount;
            }
            return null;
        }

        /// <summary>
        /// Resets the credentials state to the account with the given <paramref name="accountName"/> and the
        /// given <paramref name="projectId"/>. The <seealso cref="Reset"/> event will be raised to notify
        /// listeners on this.
        /// If <paramref name="accountName"/> cannot be found in the store then the credentials will be reset
        /// to empty.
        /// </summary>
        /// <param name="accountName">The name of the account to make current.</param>
        /// <param name="projectId">The projectId to make current.</param>
        public void ResetCredentials(string accountName, string projectId)
        {
            var newCurrentAccount = GetAccount(accountName);
            if (newCurrentAccount != null)
            {
                _currentAccount = newCurrentAccount;
                _currentProjectId = projectId;
            }
            else
            {
                Debug.WriteLine($"Unknown account: {accountName}");
                _currentAccount = null;
                _currentProjectId = null;
            }
            Reset?.Invoke(this, EventArgs.Empty);
        }

        private string GetUserAccountPath(string accountName)
        {
            StoredUserAccount stored;
            if (_cachedCredentials.TryGetValue(accountName, out stored))
            {
                return Path.Combine(s_credentialsStoreRoot, stored.FileName);
            }
            return null;
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
            try
            {
                var contents = AtomicFileRead(path);
                return JsonConvert.DeserializeObject<UserAccount>(contents);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse user account: {ex.Message}");
                throw new CredentialsStoreException(ex.Message, ex);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to read user account: {ex.Message}");
                throw new CredentialsStoreException(ex.Message, ex);
            }
        }

        private static void SaveUserAccount(UserAccount userAccount, string path)
        {
            try
            {
                var serialized = JsonConvert.SerializeObject(userAccount);
                AtomicFileWrite(path, serialized);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to save user account to {path}: {ex.Message}");
                throw new CredentialsStoreException(ex.Message, ex);
            }
        }

        private static string SaveUserAccount(UserAccount userAccount)
        {
            var name = GetFileName(userAccount);
            var savePath = Path.Combine(s_credentialsStoreRoot, name);
            SaveUserAccount(userAccount, savePath);
            return name;
        }

        private static string GetFileName(UserAccount userAccount)
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

        private void UpdateDefaultAccountProjectId()
        {
            var path = Path.Combine(s_credentialsStoreRoot, DefaultCredentialsFileName);

            if (CurrentAccount?.AccountName != null)
            {
                var defaultCredentials = new DefaultCredentials
                {
                    ProjectId = CurrentProjectId,
                    AccountName = CurrentAccount?.AccountName,
                };

                try
                {
                    Debug.WriteLine($"Updating default account: {path}");
                    AtomicFileWrite(path, JsonConvert.SerializeObject(defaultCredentials));
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"Failed to update default credentials: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"Deleting default account: {path}");
                try
                {
                    File.Delete(path);
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"Failed to delete the default credentials: {ex.Message}");
                }
            }
        }

        private DefaultCredentials LoadDefaultCredentials()
        {
            var path = Path.Combine(s_credentialsStoreRoot, DefaultCredentialsFileName);
            if (!File.Exists(path))
            {
                return null;
            }

            DefaultCredentials result = null;
            try
            {
                var contents = AtomicFileRead(path);
                result = JsonConvert.DeserializeObject<DefaultCredentials>(contents);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse default credentials: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to read default credentials: {ex.Message}");
            }
            return result;
        }

        private static void AtomicFileWrite(string path, string contents)
        {
            try
            {
                using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var stream = new StreamWriter(file))
                {
                    stream.Write(contents);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to write the file {path}: {ex.Message}");
                throw;
            }
        }

        private static string AtomicFileRead(string path)
        {
            try
            {
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var stream = new StreamReader(file))
                {
                    return stream.ReadToEnd();
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to write the file {path}: {ex.Message}");
                throw;
            }
        }
    }
}
