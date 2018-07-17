// Copyright 2017 Google Inc. All Rights Reserved.
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

using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PowerShellUtils
{
    /// <summary>
    /// Utilities for remote powershell operations.
    /// </summary>
    public static class RemotePowerShellUtils
    {
        /// <summary>
        /// Gets the embedded resource text file.
        /// </summary>
        /// <param name="resourceName">
        /// Script file is embeded as resource. To extract the file, use resource name.
        /// i.e GoogleCloudExtension.RemotePowershell.Resources.EmbededScript.ps1
        /// </param>
        /// <returns>The text content of the embeded resource file</returns>
        /// <exception cref="FileNotFoundException">The file of <paramref name="resourceName"/> is not found.</exception>
        public static string GetEmbeddedFile(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException(resourceName);
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Create <seealso cref="PSCredential"/> object from username and password.
        /// </summary>
        public static PSCredential CreatePSCredential(string user, string password)
            => new PSCredential(user, ConvertToSecureString(password));

        /// <summary>
        /// Add a variable to the PowerShell session.
        /// </summary>
        /// <param name="powerShell">The <seealso cref="PowerShell"/> object.</param>
        /// <param name="name">Variable name.</param>
        /// <param name="value">Variable value.</param>
        public static void AddVariable(this PowerShell powerShell, string name, object value)
        {
            powerShell.AddCommand("Set-Variable");
            powerShell.AddParameter("Name", name);
            powerShell.AddParameter("Value", value);
        }

        /// <summary>
        /// Convert string to secure string.
        /// </summary>
        private static SecureString ConvertToSecureString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException(nameof(input));
            }
            var output = new SecureString();
            foreach (char p in input)
            {
                output.AppendChar(p);
            }

            return output;
        }

        /// <summary>
        /// Asyncronously executes the PowerShell commands.
        /// </summary>
        /// <param name="powerShell">The <seealso cref="PowerShell"/> object.</param>
        /// <param name="cancelToken">When cancelation is requested, this method stops the execution and throws.</param>
        /// <returns>
        /// The <see cref="PSDataCollection{T}"/> returned by the powershell execution.
        /// </returns>
        public static async Task<PSDataCollection<PSObject>> InvokeAsync(
            this PowerShell powerShell,
            CancellationToken cancelToken = default(CancellationToken))
        {
            cancelToken.ThrowIfCancellationRequested();

            powerShell.InvocationStateChanged += (sender, args) =>
            {
                if (args.InvocationStateInfo.State == PSInvocationState.Running)
                {
                    cancelToken.Register(() => powerShell.BeginStop(null, null));
                }
            };

            try
            {
                return await Task.Factory.FromAsync(powerShell.BeginInvoke(), powerShell.EndInvoke);
            }
            finally
            {
                cancelToken.ThrowIfCancellationRequested();
            }
        }
    }
}
