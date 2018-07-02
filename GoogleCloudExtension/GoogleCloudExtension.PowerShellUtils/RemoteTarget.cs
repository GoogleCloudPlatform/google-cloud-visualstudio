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
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PowerShellUtils
{
    /// <summary>
    /// Reomote powershell target machine.
    /// </summary>
    public class RemoteTarget
    {
        /// <summary>
        /// Remote powershell HTTPS port id.
        /// </summary>
        private const int RemotePort = 5986;

        /// <summary>
        /// Used for WSManConnectionInfo shellUri to create remote PowerShell session.
        /// </summary>
        private const string ShellUri = @"http://schemas.microsoft.com/powershell/Microsoft.PowerShell";

        /// <summary>
        /// Used for WSManConnectionInfo appName argument to create remote PowerShell session.
        /// </summary>
        private const string AppName = @"/wsman";

        private readonly string _computerName;
        private readonly PSCredential _credential;

        /// <summary>
        /// Intializes an instance.
        /// </summary>
        /// <param name="computerName">
        /// The remote machine name. In the case name resolution does not work, 
        /// use public ip address.
        /// </param>
        /// <param name="credential">The credential for authentication to the remote target.</param>
        public RemoteTarget(string computerName, PSCredential credential)
        {
            _computerName = computerName;
            _credential = credential;
        }

        /// <summary>
        /// This method firstly establishes a session to the remote target.
        /// Then executes the <paramref name="script"/>. 
        /// When execution is done, the session is closed.
        /// </summary>
        /// <param name="script">
        /// The powershell script to be executed inside PSSession.
        /// </param>
        /// <param name="cancelToken">
        /// Long execution can be terminated by the cancelToken. 
        /// This will also terminate the PSSession.
        /// </param>
        public async Task EnterSessionExecute(string script, CancellationToken cancelToken)
        {
            if (String.IsNullOrWhiteSpace(script))
            {
                throw new ArgumentNullException(nameof(script));
            }

            AuthenticationMechanism auth = AuthenticationMechanism.Default;

            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(
                useSsl: true,
                computerName: _computerName,
                port: RemotePort,
                appName: AppName,
                shellUri: ShellUri,
                credential: _credential);
            connectionInfo.AuthenticationMechanism = auth;
            // GCE VM uses an GCP internal certificate, the root is not by default trusted.
            // We know we are connecting to GCP VM, it is safe to skip these certificate validation.
            connectionInfo.SkipCACheck = true;
            connectionInfo.SkipCNCheck = true;
            connectionInfo.SkipRevocationCheck = true;

            using (var runSpace = RunspaceFactory.CreateRunspace(connectionInfo))
            {
                runSpace.Open();

                Debug.WriteLine($"Connected to {_computerName} as {_credential.UserName}");

                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.Runspace = runSpace;
                    powerShell.AddScript(script);
                    await powerShell.InvokeAsync(cancelToken);
                }
            }
        }
    }
}
