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

using GoogleCloudExtension.PowerShellUtils;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using static GoogleCloudExtension.VsVersion.VsVersionUtils;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Start remote powershell and copy debugger remote tool to target machine.
    /// Start the debugger remote tool from within the remote powershell session.
    /// </summary>
    public class InstallStartRemoteToolStepViewModel : AttachDebuggerStepBase
    {
        private static readonly TimeSpan WaitConnectionTimeout = TimeSpan.FromMinutes(3);
        private RemoteToolInstaller _installer;
        private CancellationTokenSource _installerCancellationSource;
        private string _progressMessage;

        /// <summary>
        /// The message when operations are in progress.
        /// </summary>
        public string ProgressMessage
        {
            get { return _progressMessage; }
            set { SetValueAndRaise(out _progressMessage, value); }
        }

        public InstallStartRemoteToolStepViewModel(
            InstallStartRemoteToolStepContent content,
            AttachDebuggerContext context)
            : base(context)
        {
            Content = content;
        }

        #region Implement interface IAttachDebuggerStep
        public override ContentControl Content { get; }

        public override IAttachDebuggerStep OnCancelCommand()
        {
            _installerCancellationSource?.Cancel();
            return null;
        }

        // Okay button is not enabled at this step.
        // This method should never be called for this step.
        public override Task<IAttachDebuggerStep> OnOkCommandAsync() => Task.FromResult<IAttachDebuggerStep>(null);

        public override async Task<IAttachDebuggerStep> OnStartAsync()
        {
            IsCancelButtonEnabled = true;
            ProgressMessage = Resources.AttachDebuggerInstallSetupProgressMessage;

            _installerCancellationSource = new CancellationTokenSource();
            _installer = new RemoteToolInstaller(
                Context.PublicIp,
                Context.Credential.User,
                Context.Credential.Password,
                ToolsPathProvider.GetRemoteDebuggerToolsPath());
            if (await _installer.Install(_installerCancellationSource.Token))
            {
                var session = new RemoteToolSession(
                    Context.PublicIp,
                    Context.Credential.User,
                    Context.Credential.Password,
                    GoogleCloudExtensionPackage.Instance.SubscribeClosingEvent,
                    GoogleCloudExtensionPackage.Instance.UnsubscribeClosingEvent);

                Stopwatch watch = Stopwatch.StartNew();
                ProgressMessage = String.Format(
                    Resources.AttachDebuggerTestConnectPortMessageFormat,
                    Context.PublicIp,
                    Context.DebuggerPort.PortInfo.Port);
                while (!_installerCancellationSource.IsCancellationRequested &&
                       watch.Elapsed < WaitConnectionTimeout &&
                       !session.IsStopped)
                {
                    if (await Context.DebuggerPort.ConnectivityTest())
                    {
                        Debug.WriteLine("Connected to debuggee!");
                        return ListProcessStepViewModel.CreateStep(Context);
                    }
                    await Task.Delay(500);
                }
            }

            Context.DialogWindow.Close();
            return null; // TODO : go to help page
        }
        #endregion

        /// <summary>
        /// Creates the step that installs and starts debugger remote tool.
        /// </summary>
        public static InstallStartRemoteToolStepViewModel CreateStep(AttachDebuggerContext context)
        {
            var content = new InstallStartRemoteToolStepContent();
            var step = new InstallStartRemoteToolStepViewModel(content, context);
            content.DataContext = step;
            return step;
        }
    }
}
