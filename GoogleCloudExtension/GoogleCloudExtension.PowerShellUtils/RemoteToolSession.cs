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
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PowerShellUtils
{
    /// <summary>
    /// Open a session to a remote target and starts msvsmon.exe.
    /// For more detail, please also refer to file Resources.StartRemoteTool.ps1.
    /// </summary>
    public class RemoteToolSession
    {
        private const string StartPsFilePath = "GoogleCloudExtension.PowerShellUtils.Resources.StartRemoteTool.ps1";
        private readonly RemoteTarget _target;
        private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private readonly Task _powerShellTask;

        /// <summary>
        /// Check if the powershell task is stopped.
        /// </summary>
        public bool IsStopped => _powerShellTask.IsCompleted || _powerShellTask.IsCanceled || _powerShellTask.IsFaulted;

        /// <summary>
        /// Start the session and start the remote tool.
        /// </summary>
        /// <param name="computerName">The ipaddress of debugging target machine.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        public RemoteToolSession(
            string computerName, 
            string username, 
            string password)
        {
            _target = new RemoteTarget(computerName, username, PsUtils.ConvertToSecureString(password));
            string script = PsUtils.GetScript(StartPsFilePath);

            // TODO: Stop the session before solution exits.
            _powerShellTask = Task.Run(() =>
            {
                _target.EnterSessionExecute(script, _cancelTokenSource.Token);
            });
        }
    }
}
