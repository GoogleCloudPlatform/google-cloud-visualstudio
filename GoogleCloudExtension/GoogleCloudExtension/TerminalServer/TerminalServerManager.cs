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
    internal class TerminalServerManager
    {
        public static void OpenSession(Instance instance, WindowsInstanceCredentials credentials)
        {
            var rdpPath = CreateRdpFile(instance, credentials);
            Debug.WriteLine($"Saved .rdp file at {rdpPath}");
            Process.Start("mstsc", rdpPath);
        }

        private static string CreateRdpFile(Instance instance, WindowsInstanceCredentials credentials)
        {
            var instanceRootPath = WindowsCredentialsStore.Default.GetStoragePathForInstance(instance);
            var rdpPath = Path.Combine(instanceRootPath, GetRdpFileName(credentials));

            WriteRdpFile(rdpPath, instance, credentials);
            return rdpPath;
        }

        public static void WriteRdpFile(string path, Instance instance, WindowsInstanceCredentials credentials)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine($"full address:s:{instance.GetPublicIpAddress()}");
                writer.WriteLine($"username:s:{credentials.User}");
                writer.WriteLine($"password 51:b:{SerializePassword(credentials.Password)}");
            }
        }

        private static string SerializePassword(string password)
        {
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
