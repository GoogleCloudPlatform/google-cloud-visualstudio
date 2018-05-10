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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GoogleCloudExtension.TerminalServer
{
    /// <summary>
    /// This class manages the terminal server connection that are created to connect
    /// to the Windows VM with a the given set of credentials.
    /// </summary>
    internal class TerminalServerManager
    {
        /// <summary>
        /// Opens a new Terminal Server session against the given <paramref name="instance"/> with the
        /// given set of <paramref name="credentials"/>.
        /// </summary>
        /// <param name="instance">The Windows VM</param>
        /// <param name="credentials">The credentials to use to connect.</param>
        public static void OpenSession(Instance instance, WindowsInstanceCredentials credentials)
        {
            var rdpPath = CreateRdpFile(instance, credentials);
            Debug.WriteLine($"Saved .rdp file at {rdpPath}");
            Process.Start("mstsc", $"\"{rdpPath}\"");
        }

        private static string CreateRdpFile(Instance instance, WindowsInstanceCredentials credentials)
        {
            var instanceRootPath = WindowsCredentialsStore.Default.GetStoragePathForInstance(instance);
            var rdpPath = Path.Combine(instanceRootPath, GetRdpFileName(credentials));

            WriteRdpFile(rdpPath, instance, credentials);
            return rdpPath;
        }

        /// <summary>
        /// Creates an .rdp file with the minimal set of settings required to connect to the VM.
        /// </summary>
        public static void WriteRdpFile(string path, Instance instance, WindowsInstanceCredentials credentials)
        {
            using (var writer = new StreamWriter(path))
            {
                // The IP (or name) of the VM to connect to.
                writer.WriteLine($"full address:s:{instance.GetFullyQualifiedDomainName()}");

                // The user name to use.
                writer.WriteLine($"username:s:{credentials.User}");

                // The encrypted password to use.
                writer.WriteLine($"password 51:b:{EncryptPassword(credentials.Password)}");
            }
        }

        /// <summary>
        /// Encrypts the given password in a form that mstsc.exe will accept.
        /// </summary>
        /// <param name="password">The password to encrypt.</param>
        /// <returns>The hexadecimal string representation of the encrypted password.</returns>
        private static string EncryptPassword(string password)
        {
            // The password is encoded in Unicode first, then encrypted using the user's key. This is the value
            // that mstsc.exe expects for a locally stored password.
            var output = ProtectedData.Protect(
                Encoding.Unicode.GetBytes(password),
                null,
                DataProtectionScope.CurrentUser);
            return AsHexadecimalString(output);
        }

        private static string AsHexadecimalString(byte[] src)
        {
            StringBuilder result = new StringBuilder();
            foreach (var b in src)
            {
                result.AppendFormat("{0:x2}", b);
            }
            return result.ToString();
        }

        private static string GetRdpFileName(WindowsInstanceCredentials credentials) => $"{credentials.User}.rdp";
    }
}
