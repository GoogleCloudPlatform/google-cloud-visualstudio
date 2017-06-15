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
using System.Text;
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
        /// Executes a PowerShell script asynchronously.
        /// </summary>
        /// <param name="addCommandsCallback">Callback to add the powershell commands.</param>
        /// <param name="cancelToken">
        /// Long execution can be terminated by the cancelToken. 
        /// </param>
        /// <returns>
        /// True: successfully completed the script execution.
        /// False: Received some error in script execution or the execution is cancelled.
        /// </returns>
        public async Task<bool> ExecuteAsync(
            Action<PowerShell> addCommandsCallback,
            CancellationToken cancelToken)
        {
            using (PowerShell powerShell = PowerShell.Create())
            {
                addCommandsCallback(powerShell);
                return await Task.Run(() => WaitComplete(powerShell, cancelToken));
            }
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
        public void EnterSessionExecute(string script, CancellationToken cancelToken)
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
                    WaitComplete(powerShell, cancelToken);
                }
            }
        }

        /// <summary>
        /// Executes the PowerShell commands and waits utill it is complete 
        /// or cancelled by <paramref name="cancelToken"/>.
        /// </summary>
        /// <param name="powerShell">The <seealso cref="PowerShell"/> object.</param>
        /// <param name="cancelToken">Cancel a long running command.</param>
        /// <returns>
        /// True: successfully completed the script execution.
        /// False: Received some error in script execution or the execution is cancelled.
        /// </returns>
        private bool WaitComplete(PowerShell powerShell, CancellationToken cancelToken)
        {
            var iAsyncResult = powerShell.BeginInvoke();
            int returnIndex = WaitHandle.WaitAny(new[] { iAsyncResult.AsyncWaitHandle, cancelToken.WaitHandle });
            Debug.WriteLine($"Execution has stopped. The pipeline state: {powerShell.InvocationStateInfo.State}");
            if (cancelToken.IsCancellationRequested || returnIndex != 0 || !iAsyncResult.IsCompleted)
            {
                return false;
            }
            var outputCollection = powerShell.EndInvoke(iAsyncResult);
            PrintOutput(outputCollection);
            return !powerShell.HadErrors;
        }

        private void PrintOutput(PSDataCollection<PSObject> outputCollection)
        {
            // TODO: Write to Output window
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject psObject in outputCollection)
            {
                stringBuilder.AppendLine(psObject.ToString());
            }
            Debug.WriteLine(stringBuilder.ToString());
        }
    }
}
