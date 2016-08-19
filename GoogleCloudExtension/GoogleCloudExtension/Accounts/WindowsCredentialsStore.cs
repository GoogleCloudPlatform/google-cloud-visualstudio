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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GoogleCloudExtension.Accounts
{
    /// <summary>
    /// This class manages the Windows credentials associated with a particular VM.
    /// 
    /// The credentials are stored in files with the name {username}.data. The name of the file
    /// is the username, the contents of the file is the encrypted version of the password. Only the
    /// current user will be able to decrypt the password.
    /// 
    /// The password is encrypted/decryped using the <seealso cref="ProtectedData"/> class.
    /// </summary>
    internal class WindowsCredentialsStore
    {
        private const string WindowsCredentialsPath = @"googlecloudvsextension\windows_credentials";
        private const string PasswordFileExtension = ".data";

        private static readonly Lazy<WindowsCredentialsStore> s_defaultStore = new Lazy<WindowsCredentialsStore>();
        private static readonly string s_credentialsStoreRoot = GetCredentialsStoreRoot();

        /// <summary>
        /// In memory cache of the credentials for the current credentials (account and project pair).
        /// </summary>
        private readonly Dictionary<string, IEnumerable<WindowsCredentials>> _credentialsForInstance = new Dictionary<string, IEnumerable<WindowsCredentials>>();

        public static WindowsCredentialsStore Default => s_defaultStore.Value;

        /// <summary>
        /// Loads the list of Windows credentials associated with <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>The list of <seealso cref="WindowsCredentials"/> associated with the instance. It might be
        /// empty if no credentials are found.</returns>
        public IEnumerable<WindowsCredentials> GetCredentialsForInstance(Instance instance)
        {
            var instancePath = GetInstancePath(instance);
            IEnumerable<WindowsCredentials> result;
            if (_credentialsForInstance.TryGetValue(instancePath, out result))
            {
                return result;
            }

            var fullInstancePath = Path.Combine(s_credentialsStoreRoot, instancePath);
            if (!Directory.Exists(fullInstancePath))
            {
                result = Enumerable.Empty<WindowsCredentials>();
            }
            else
            {
                result = Directory.EnumerateFiles(fullInstancePath)
                    .Where(x => Path.GetExtension(x) == PasswordFileExtension)
                    .Select(x => LoadEncryptedCredentials(x))
                    .OrderBy(x => x.UserName);
            }
            _credentialsForInstance[instancePath] = result;

            return result;
        }

        /// <summary>
        /// Adds a Windows credential to the store for the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="credentials">The credentials to store.</param>
        public void AddCredentialsToInstance(Instance instance, WindowsCredentials credentials)
        {
            var instancePath = GetInstancePath(instance);
            var fullInstancePath = Path.Combine(s_credentialsStoreRoot, instancePath);

            SaveEncryptedCredentials(fullInstancePath, credentials);
            _credentialsForInstance.Remove(instancePath);
        }

        /// <summary>
        /// Deletes the given credentials from the list of associated credenials for <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="credentials">The credentials.</param>
        public void DeleteCredentialsForInstance(Instance instance, WindowsCredentials credentials)
        {
            var instancePath = GetInstancePath(instance);
            var fullInstancePath = Path.Combine(s_credentialsStoreRoot, instancePath);
            var credentialsPath = Path.Combine(fullInstancePath, GetFileName(credentials));

            if (File.Exists(credentialsPath))
            {
                File.Delete(credentialsPath);
                _credentialsForInstance.Remove(instancePath);
            }
        }

        private WindowsCredentials LoadEncryptedCredentials(string path)
        {
            var userName = GetUserName(path);
            var encryptedPassword = File.ReadAllBytes(path);
            var passwordBytes = ProtectedData.Unprotect(encryptedPassword, null, DataProtectionScope.CurrentUser);

            return new WindowsCredentials
            {
                UserName = userName,
                Password = Encoding.UTF8.GetString(passwordBytes)
            };
        }

        private void SaveEncryptedCredentials(string path, WindowsCredentials credentials)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var filePath = Path.Combine(path, GetFileName(credentials));
            var passwordBytes = Encoding.UTF8.GetBytes(credentials.Password);
            var encrypted = ProtectedData.Protect(passwordBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(filePath, encrypted);
        }

        private static string GetCredentialsStoreRoot()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, WindowsCredentialsPath);
        }

        private static string GetInstancePath(Instance instance)
        {
            var credentials = CredentialsStore.Default;
            return $@"{credentials.CurrentProjectId}\{instance.GetZoneName()}\{instance.Name}";
        }

        private static string GetFileName(WindowsCredentials credentials) => $"{credentials.UserName}{PasswordFileExtension}";

        private static string GetUserName(string path) => Path.GetFileNameWithoutExtension(path);
    }
}
