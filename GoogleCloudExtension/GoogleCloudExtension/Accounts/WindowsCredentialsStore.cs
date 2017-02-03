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
using GoogleCloudExtension.GCloud;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
        private const string WindowsInstanceCredentialsPath = @"googlecloudvsextension\windows_credentials";
        private const string PasswordFileExtension = ".data";

        private static readonly Lazy<WindowsCredentialsStore> s_defaultStore = new Lazy<WindowsCredentialsStore>();
        private static readonly string s_credentialsStoreRoot = GetCredentialsStoreRoot();
        private static Regex s_invalidNameCharPattern = new Regex("[;:\\?\\\\]");

        /// <summary>
        /// In memory cache of the credentials for the current credentials (account and project pair).
        /// </summary>
        private readonly Dictionary<string, IEnumerable<WindowsInstanceCredentials>> _credentialsForInstance = new Dictionary<string, IEnumerable<WindowsInstanceCredentials>>();

        public static WindowsCredentialsStore Default => s_defaultStore.Value;

        /// <summary>
        /// Loads the list of Windows credentials associated with <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM</param>
        /// <returns>The list of <seealso cref="WindowsInstanceCredentials"/> associated with The GCE VM. It might be
        /// empty if no credentials are found.</returns>
        public IEnumerable<WindowsInstanceCredentials> GetCredentialsForInstance(Instance instance)
        {
            var instancePath = GetInstancePath(instance);
            IEnumerable<WindowsInstanceCredentials> result;
            if (_credentialsForInstance.TryGetValue(instancePath, out result))
            {
                return result;
            }

            var instanceStoragePath = GetStoragePathForInstance(instance);
            if (!Directory.Exists(instanceStoragePath))
            {
                result = Enumerable.Empty<WindowsInstanceCredentials>();
            }
            else
            {
                result = Directory.EnumerateFiles(instanceStoragePath)
                    .Where(x => Path.GetExtension(x) == PasswordFileExtension)
                    .Select(x => LoadEncryptedCredentials(x))
                    .OrderBy(x => x.User);
            }
            _credentialsForInstance[instancePath] = result;

            return result;
        }

        /// <summary>
        /// Adds a Windows credential to the store for the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <param name="credentials">The credentials to store.</param>
        public void AddCredentialsToInstance(Instance instance, WindowsInstanceCredentials credentials)
        {
            var instancePath = GetInstancePath(instance);
            var instanceStoragePath = GetStoragePathForInstance(instance);

            SaveEncryptedCredentials(instanceStoragePath, credentials);
            _credentialsForInstance.Remove(instancePath);
        }

        /// <summary>
        /// Deletes the given credentials from the list of associated credenials for <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <param name="credentials">The credentials.</param>
        public void DeleteCredentialsForInstance(Instance instance, WindowsInstanceCredentials credentials)
        {
            var instancePath = GetInstancePath(instance);
            var instanceStoragePath = GetStoragePathForInstance(instance);
            var credentialsPath = Path.Combine(instanceStoragePath, GetFileName(credentials));

            if (File.Exists(credentialsPath))
            {
                File.Delete(credentialsPath);
                _credentialsForInstance.Remove(instancePath);
            }
        }

        /// <summary>
        /// Returns the path where to store credential related information for a GCE VM.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <returns>The full path where to store information for the instance.</returns>
        public string GetStoragePathForInstance(Instance instance)
        {
            var instancePath = GetInstancePath(instance);
            return Path.Combine(s_credentialsStoreRoot, instancePath);
        }

        private WindowsInstanceCredentials LoadEncryptedCredentials(string path)
        {
            var userName = GetUserName(path);
            var encryptedPassword = File.ReadAllBytes(path);
            var passwordBytes = ProtectedData.Unprotect(encryptedPassword, null, DataProtectionScope.CurrentUser);

            return new WindowsInstanceCredentials { User = userName, Password = Encoding.UTF8.GetString(passwordBytes) };
        }

        private void SaveEncryptedCredentials(string path, WindowsInstanceCredentials credentials)
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
            return Path.Combine(localAppData, WindowsInstanceCredentialsPath);
        }

        private static string GetInstancePath(Instance instance)
        {
            var credentials = CredentialsStore.Default;
            return $@"{ToValidPathName(credentials.CurrentProjectId)}\{ToValidPathName(instance.GetZoneName())}\{ToValidPathName(instance.Name)}";
        }

        private static string ToValidPathName(string name) => s_invalidNameCharPattern.Replace(name, "_");

        private static string GetFileName(WindowsInstanceCredentials credentials) => $"{credentials.User}{PasswordFileExtension}";

        private static string GetUserName(string path) => Path.GetFileNameWithoutExtension(path);
    }
}
