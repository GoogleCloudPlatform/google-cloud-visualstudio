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
        /// <summary>
        /// The resource name to get embedded script file Resources.StartRemoteTools.ps1.
        /// </summary>
        private const string StartPsFilePath = "GoogleCloudExtension.PowerShellUtils.Resources.StartRemoteTool.ps1";

        private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private readonly Task _powerShellTask;
        private readonly EventHandler _closingEventHandler;

        /// <summary>
        /// Check if the powershell task is stopped.
        /// </summary>
        public bool IsStopped => _powerShellTask.IsCompleted || _powerShellTask.IsCanceled || _powerShellTask.IsFaulted;

        /// <summary>
        /// Start the session and start the remote tool.
        /// This starts a backgroud task and leave it running till it is cancelled or stopped. 
        /// </summary>
        /// <param name="computerName">The ipaddress of debugging target machine.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="subscribeClosingEvent">The method to subscribe to solution closing event.</param>
        /// <param name="unsubscribeClosingEvent">The method to unsubscribe solution closing event.</param>
        public RemoteToolSession(
            string computerName,
            string username,
            string password,
            Action<EventHandler> subscribeClosingEvent,
            Action<EventHandler> unsubscribeClosingEvent)
        {
            var target = new RemoteTarget(computerName, RemotePowerShellUtils.CreatePSCredential(username, password));
            string script = RemotePowerShellUtils.GetEmbeddedFile(StartPsFilePath);
            _closingEventHandler = (se, e) => Stop();
            subscribeClosingEvent(_closingEventHandler);

            _powerShellTask = ExecuteScript(target, script, unsubscribeClosingEvent);
        }

        /// <summary>
        /// Stop the session at VS shutting down event.
        /// </summary>
        private void Stop()
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


        private async Task ExecuteScript(RemoteTarget target, string script, Action<EventHandler> unsubscribeClosingEvent)
        {
            try
            {
                await target.EnterSessionExecute(script, _cancelTokenSource.Token);
            }
            finally
            {
                unsubscribeClosingEvent(_closingEventHandler);
            }
        }
    }
}
