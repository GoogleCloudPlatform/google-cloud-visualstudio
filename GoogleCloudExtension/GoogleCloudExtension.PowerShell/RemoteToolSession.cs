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
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PowerShellUtils
{
    /// <summary>
    /// Open a session to a remote target and starts msvsmon.exe.
    /// </summary>
    public class RemoteToolSession
    {
        private readonly RemoteTarget _target;
        private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private readonly Task _powerShellTask;
        private EventHandler _handler;

        /// <summary>
        /// Check if the powershell task is stopped.
        /// </summary>
        public bool IsStopped => _powerShellTask.IsCompleted || _powerShellTask.IsCanceled || _powerShellTask.IsFaulted;

        /// <summary>
        /// Start the session and start the remote tool.
        /// </summary>
        /// <param name="computerName">The ipaddress of debugging target machine.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The secure password for authentication.</param>
        public RemoteToolSession(
            string computerName, 
            string username, 
            SecureString password, 
            Action<EventHandler> subscribe,
            Action<EventHandler> unsubscribe )
        {
            _target = new RemoteTarget(computerName, username, password);
            string script = PsUtils.GetScript("GoogleCloudExtension.RemotePowerShell.Resources.StartRemoteTool.ps1");
            _handler = (se, e) => Stop();
            subscribe(_handler);
            _powerShellTask = Task.Run(() =>
            {
                _target.EnterSessionExecute(script, _cancelTokenSource.Token);
                unsubscribe(_handler);
            });
        }

        /// <summary>
        /// Stop the session at VS shutting down event.
        /// </summary>
        public void Stop()
        {
            if (!IsStopped)
            {
                Debug.WriteLine($"_cancelTokenSource.Cancel() ");
                _cancelTokenSource.Cancel();
            }
            Debug.WriteLine($"_powerShellTask.Wait() ");
            _powerShellTask.Wait();
            Debug.WriteLine($"_powerShellTask.Wait() complete.");
        }
    }
}
