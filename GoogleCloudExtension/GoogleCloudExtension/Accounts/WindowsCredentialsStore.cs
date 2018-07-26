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
using GoogleCloudExtension.Services;
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
    internal class WindowsCredentialsStore : IWindowsCredentialsStore
    {
        private const string WindowsInstanceCredentialsPath = @"googlecloudvsextension\windows_credentials";
        public const string PasswordFileExtension = ".data";

        private static readonly Lazy<WindowsCredentialsStore> s_defaultStore =
            new Lazy<WindowsCredentialsStore>(() => new WindowsCredentialsStore());
        internal static readonly string s_credentialsStoreRoot = GetCredentialsStoreRoot();
        private static readonly Regex s_invalidNameCharPattern = new Regex("[;:\\?\\\\]");

        public static WindowsCredentialsStore Default => s_defaultStore.Value;

        /// Mockable static methods for testing.
        internal Func<string, bool> DirectoryExists { private get; set; } = Directory.Exists;
        internal Func<string, bool> FileExists { private get; set; } = File.Exists;
        internal Func<string, IEnumerable<string>> EnumerateFiles { private get; set; } = Directory.EnumerateFiles;
        internal Func<string, byte[]> ReadAllBytes { private get; set; } = File.ReadAllBytes;
        internal Action<string, byte[]> WriteAllBytes { private get; set; } = File.WriteAllBytes;
        internal Func<string, DirectoryInfo> CreateDirectory { private get; set; } = Directory.CreateDirectory;
        internal Action<string> DeleteFile { private get; set; } = File.Delete;

        /// <summary>
        /// Loads the list of Windows credentials associated with <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM</param>
        /// <returns>The list of <seealso cref="WindowsInstanceCredentials"/> associated with The GCE VM. It might be
        /// empty if no credentials are found.</returns>
        public IList<WindowsInstanceCredentials> GetCredentialsForInstance(Instance instance)
        {
            IEnumerable<WindowsInstanceCredentials> result;

            string instanceStoragePath = GetStoragePathForInstance(instance);
            if (!DirectoryExists(instanceStoragePath))
            {
                result = Enumerable.Empty<WindowsInstanceCredentials>();
            }
            else
            {
                result = EnumerateFiles(instanceStoragePath)
                    .Where(x => Path.GetExtension(x) == PasswordFileExtension)
                    .Select(LoadEncryptedCredentials)
                    .Where(x => x != null)
                    .OrderBy(x => x.User);
            }

            return result.ToList();
        }

        /// <summary>
        /// Adds a Windows credential to the store for the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <param name="credentials">The credentials to store.</param>
        public void AddCredentialsToInstance(Instance instance, WindowsInstanceCredentials credentials)
        {
            string instanceStoragePath = GetStoragePathForInstance(instance);

            SaveEncryptedCredentials(instanceStoragePath, credentials);
        }

        /// <summary>
        /// Deletes the given credentials from the list of associated credenials for <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <param name="credentials">The credentials.</param>
        public void DeleteCredentialsForInstance(Instance instance, WindowsInstanceCredentials credentials)
        {
            string instanceStoragePath = GetStoragePathForInstance(instance);
            string credentialsPath = Path.Combine(instanceStoragePath, GetFileName(credentials));

            if (FileExists(credentialsPath))
            {
                DeleteFile(credentialsPath);
            }
        }

        /// <summary>
        /// Returns the path where to store credential related information for a GCE VM.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <returns>The full path where to store information for the instance.</returns>
        public string GetStoragePathForInstance(Instance instance)
        {
            string instancePath = GetInstancePath(instance);
            return Path.Combine(s_credentialsStoreRoot, instancePath);
        }

        /// <summary>
        /// Attempts to load and decrypt the credentials stored in <paramref name="path"/> and returns an
        /// <seealso cref="WindowsInstanceCredentials"/> instance with the information stored in the file. If
        /// the file cannot be loaded or decrypted it will return null.
        /// 
        /// Note: The function will attempt to delete the file if it cannot be decrypted, this typically means that
        /// the user's key is no longer valid. The function does not attempt to delete the file in case of a <see cref="IOException"/>
        /// since that will probably also throw again.
        /// </summary>
        private WindowsInstanceCredentials LoadEncryptedCredentials(string path)
        {
            try
            {
                string userName = GetUserName(path);
                byte[] encryptedPassword = ReadAllBytes(path);
                byte[] passwordBytes = ProtectedData.Unprotect(
                    encryptedPassword, null, DataProtectionScope.CurrentUser);

                return new WindowsInstanceCredentials(userName, Encoding.UTF8.GetString(passwordBytes));
            }
            catch (CryptographicException cryptographicException)
            {
                bool delete = UserPromptService.Default.ErrorActionPrompt(
                    string.Format(Resources.WindowsCredentialsStoreDecryptErrorMessage, path),
                    Resources.WindowsCredentialsStoreDecryptionErrorTitle, cryptographicException.ToString());
                if (delete)
                {
                    try
                    {
                        DeleteFile(path);
                    }
                    catch (IOException ioException)
                    {
                        UserPromptService.Default.ErrorPrompt(
                            string.Format(Resources.WindowsCredentialsStoreDeletingCorruptedErrorMessage, path),
                            Resources.WindowsCredentialsStoreDeletingCorruptedErrorTitle, ioException.ToString());
                    }
                }
                return null;
            }
            catch (IOException e)
            {
                UserPromptService.Default.ErrorPrompt(
                    string.Format(Resources.WindowsCredentialsStoreCredentialFileLoadErrorMessage, path),
                    Resources.WindowsCredentialsStoreCredentialFileLoadErrorTitle, e.ToString());
                return null;
            }
        }

        private void SaveEncryptedCredentials(string path, WindowsInstanceCredentials credentials)
        {
            if (!DirectoryExists(path))
            {
                CreateDirectory(path);
            }

            string filePath = Path.Combine(path, GetFileName(credentials));
            byte[] passwordBytes = Encoding.UTF8.GetBytes(credentials.Password);
            byte[] encrypted = ProtectedData.Protect(passwordBytes, null, DataProtectionScope.CurrentUser);
            WriteAllBytes(filePath, encrypted);
        }

        private static string GetCredentialsStoreRoot()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, WindowsInstanceCredentialsPath);
        }

        private string GetInstancePath(Instance instance)
        {
            ICredentialsStore credentials = CredentialsStore.Default;
            return $@"{ToValidPathName(credentials.CurrentProjectId)}\{ToValidPathName(instance.GetZoneName())}\{ToValidPathName(instance.Name)}";
        }

        private static string ToValidPathName(string name) => s_invalidNameCharPattern.Replace(name, "_");

        private static string GetFileName(WindowsInstanceCredentials credentials) => $"{credentials.User}{PasswordFileExtension}";

        private static string GetUserName(string path) => Path.GetFileNameWithoutExtension(path);
    }
}
