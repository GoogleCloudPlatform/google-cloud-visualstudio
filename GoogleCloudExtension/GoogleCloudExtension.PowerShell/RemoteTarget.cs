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
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Security;
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
        private const int RemotePort = 5986;
        private const string ShellURI = @"http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
        private const string AppName = @"/wsman";

        private readonly string _computerName;
        private readonly string _username;
        private readonly SecureString _password;
        private readonly PSCredential _credential;

        /// <summary>
        /// Intializes an instance.
        /// </summary>
        /// <param name="computerName">
        /// The remote machine name. In the case name resolution does not work, 
        /// use public ip address.
        /// </param>
        /// <param name="username">The username of the credential to access the target machine.</param>
        /// <param name="securePassword">The password of the credential to to access the target machine.</param>
        public RemoteTarget(string computerName, string username, SecureString securePassword)
        {
            _computerName = computerName;
            _username = username;
            _password = securePassword;
            _credential = PsUtils.CreatePSCredential(username, securePassword);
        }

        /// <summary>
        /// Executes a PowerShell script asynchronously.
        /// </summary>
        /// <param name="addCommndsCallback">Callback to add the powershell commands.</param>
        /// <param name="cancelToken">
        /// Long execution can be terminated by the cancelToken. 
        /// </param>
        public Task<bool> ExecuteAsync(
            Action<PowerShell> addCommndsCallback,
            CancellationToken cancelToken)
        {
            using (PowerShell powerShell = PowerShell.Create())
            {
                addCommndsCallback(powerShell);
                return Task.Run(() => WaitComplete(powerShell, cancelToken));
            }
        }

        /// <summary>
        /// This method firstly establish a session to the remote target.
        /// Then execute the <paramref name="script"/>. 
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
                shellUri: ShellURI,
                credential: _credential);
            connectionInfo.AuthenticationMechanism = auth;
            connectionInfo.SkipCACheck = true;
            connectionInfo.SkipCNCheck = true;
            connectionInfo.SkipRevocationCheck = true;

            using (var runSpace = RunspaceFactory.CreateRunspace(connectionInfo))
            {
                var psh = runSpace.CreatePipeline();
                runSpace.Open();

                Debug.WriteLine($"Connected to {_computerName} as {_username}");

                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.Runspace = runSpace;
                    powerShell.AddScript(script);
                    WaitComplete(powerShell, cancelToken);
                }
            }
        }

        private bool WaitComplete(PowerShell powerShell, CancellationToken cancelToken)
        {
            PSDataCollection<PSObject> outputCollection = new PSDataCollection<PSObject>();
            outputCollection.DataAdded += OnDataAdded;
            powerShell.Streams.Error.DataAdded += OnErrorAdded;

            var iAsyncResult = powerShell.BeginInvoke<PSObject, PSObject>(input: null, output: outputCollection);
            int returnid = WaitHandle.WaitAny(new[] { iAsyncResult.AsyncWaitHandle, cancelToken.WaitHandle });
            Debug.WriteLine($"Execution has stopped. The pipeline state: {powerShell.InvocationStateInfo.State}");
            if (cancelToken.IsCancellationRequested || returnid != 0 || !iAsyncResult.IsCompleted)
            {
                return false;
            }
            powerShell.EndInvoke(iAsyncResult);

            // TODO: Write to Output window
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject psObject in outputCollection)
            {
                stringBuilder.AppendLine(psObject.ToString());
            }
            Debug.WriteLine(stringBuilder.ToString());

            return (powerShell.Streams.Error?.Count).GetValueOrDefault() == 0;
        }

        /// <summary>
        /// Event handler for when data is added to the output stream.
        /// </summary>
        /// <param name="sender">PSDataCollection of all output items.</param>
        /// <param name="e">Contains the index ID of the added collection item 
        /// and the ID of the PowerShell instance this event belongs to.</param>
        private static void OnDataAdded(object sender, DataAddedEventArgs e)
        {
            // TODO: print data in Output window
            Debug.WriteLine("Object added to output.");
        }

        /// <summary>
        /// Event handler for when Data is added to the Error stream.
        /// </summary>
        /// <param name="sender">PSDataCollection of all error output items.</param>
        /// <param name="e">
        /// Contains the index ID of the added collection item 
        /// and the ID of the PowerShell instance this event belongs to.
        /// </param>
        private static void OnErrorAdded(object sender, DataAddedEventArgs e)
        {
            // TODO: print data in Output window
            Debug.WriteLine("An error was written to the Error stream!");
        }
    }
}
