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
using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
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

        public static CredentialsStore Default => s_defaultCredentialsStore.Value;

        public event EventHandler CurrentAccountChanged;
        public event EventHandler CurrentProjectIdChanged;
        public event EventHandler CurrentProjectNumericIdChanged;
        public event EventHandler Reset;

        /// <summary>
        /// The current <see cref="UserAccount"/> selected.
        /// </summary>
        public UserAccount CurrentAccount { get; private set; }

        /// <summary>
        /// Returns the path for the current account.
        /// </summary>
        public string CurrentAccountPath => GetUserAccountPath(CurrentAccount.AccountName);

        /// <summary>
        /// The GoogleCredential for the current <see cref="UserAccount"/>.
        /// </summary>
        public GoogleCredential CurrentGoogleCredential => CurrentAccount?.GetGoogleCredential();

        /// <summary>
        /// The currently selected project ID.
        /// </summary>
        public string CurrentProjectId { get; private set; }

        /// <summary>
        /// The currently selected project numeric ID, might be null if no project is loaded.
        /// </summary>
        public string CurrentProjectNumericId { get; private set; }

        /// <summary>
        /// The cached list of GCP projects for the current project.
        /// </summary>
        public Task<IEnumerable<Project>> CurrentAccountProjects { get; private set; }

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
        /// Updates the current project data from the given <paramref name="project"/>.
        /// </summary>
        public void UpdateCurrentProject(Project project)
        {
            if (project?.ProjectId != CurrentProjectId)
            {
                CurrentProjectId = project?.ProjectId;
                CurrentProjectNumericId = project?.ProjectNumber.ToString();

                UpdateDefaultCredentials();
                CurrentProjectIdChanged?.Invoke(this, EventArgs.Empty);
                CurrentProjectNumericIdChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Updates the current account for the extension. This method will also invalidate the project
        /// it is up to the caller to select an appropriate one.
        /// </summary>
        /// <param name="account"></param>
        public void UpdateCurrentAccount(UserAccount account)
        {
            if (CurrentAccount?.AccountName != account?.AccountName)
            {
                CurrentAccount = account;
                CurrentProjectId = null;
                CurrentProjectNumericId = null;

                InvalidateProjectList();

                UpdateDefaultCredentials();
                CurrentAccountChanged?.Invoke(this, EventArgs.Empty);
            }
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
                CurrentAccount = newCurrentAccount;
                CurrentProjectId = projectId;
                CurrentProjectNumericId = null;
            }
            else
            {
                Debug.WriteLine($"Unknown account: {accountName}");
                CurrentAccount = null;
                CurrentProjectId = null;
                CurrentProjectNumericId = null;
            }
            Reset?.Invoke(this, EventArgs.Empty);

            InvalidateProjectList();
        }

        /// <summary>
        /// Refreshes the list of projects for the current account.
        /// </summary>
        public void RefreshProjects()
        {
            InvalidateProjectList();
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
                Debug.WriteLine($"Should not be here, unknown account name: {account.AccountName}");
                throw new InvalidOperationException($"Unknown account name: {account.AccountName}");
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

            StoredUserAccount result = null;
            _cachedCredentials.TryGetValue(accountName, out result);
            return result?.UserAccount;
        }

        private void InvalidateProjectList()
        {
            Debug.WriteLine("Starting to load projects.");
            CurrentAccountProjects = LoadCurrentAccountProjectsAsync();
        }

        private async Task<IEnumerable<Project>> LoadCurrentAccountProjectsAsync()
        {
            try
            {
                var dataSource = new ResourceManagerDataSource(
                    CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.VersionedApplicationName);
                return await dataSource.GetProjectsListAsync();
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                return Enumerable.Empty<Project>();
            }
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
                .ToDictionary(x => x.UserAccount.AccountName);
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

        private void UpdateDefaultCredentials()
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
                Debug.WriteLine($"Failed to read the file {path}: {ex.Message}");
                throw;
            }
        }
    }
}
