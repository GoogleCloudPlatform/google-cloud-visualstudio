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

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.PowerShellUtils;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
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
        private static readonly TimeSpan s_waitConnectionTimeout = TimeSpan.FromMinutes(3);

        private readonly RemoteToolInstaller _installer;
        private string _progressMessage;

        /// <summary>
        /// The message when operations are in progress.
        /// </summary>
        public string ProgressMessage
        {
            get { return _progressMessage; }
            set { SetValueAndRaise(ref _progressMessage, value); }
        }

        #region Implement interface IAttachDebuggerStep
        public override ContentControl Content { get; }

        // Okay button is not enabled at this step.
        // This method should never be called for this step.
        public override Task<IAttachDebuggerStep> OnOkCommandAsync() => Task.FromResult<IAttachDebuggerStep>(null);

        public override async Task<IAttachDebuggerStep> OnStartAsync()
        {
            IsCancelButtonEnabled = true;
            ProgressMessage = Resources.AttachDebuggerInstallSetupProgressMessage;

            bool installed = false;

            var startTimestamp = DateTime.Now;

            // The following try catch is for adding Analytics in failure case.
            try
            {
                installed = await _installer.Install(CancelToken);
            }
            catch (PowerShellFailedToConnectException)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.AttachDebuggerConnectionFailedMessage,
                    title: Resources.UiDefaultPromptTitle);
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                Debug.WriteLine($"{ex}");
                UserPromptUtils.ErrorPrompt(
                    message: Resources.AttachDebuggerInstallerError,
                    title: Resources.UiDefaultPromptTitle,
                    errorDetails: ex.Message);
            }

            if (installed)
            {
                EventsReporterWrapper.ReportEvent(RemoteDebuggerRemoteToolsInstalledEvent.Create(
                    CommandStatus.Success, DateTime.Now - startTimestamp));
            }
            else if (!CancelToken.IsCancellationRequested)
            {
                EventsReporterWrapper.ReportEvent(
                    RemoteDebuggerRemoteToolsInstalledEvent.Create(CommandStatus.Failure));
            }

            if (installed)
            {
                // Reset start time to measure performance of starting remote tools
                startTimestamp = DateTime.Now;

                RemoteToolSession session;

                // The following try catch is for adding analytics where there is exception
                try
                {
                    session = new RemoteToolSession(
                        Context.PublicIp,
                        Context.Credential.User,
                        Context.Credential.Password,
                        GoogleCloudExtensionPackage.Instance.SubscribeClosingEvent,
                        GoogleCloudExtensionPackage.Instance.UnsubscribeClosingEvent);
                }
                catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
                {
                    EventsReporterWrapper.ReportEvent(
                        RemoteDebuggerRemoteToolsStartedEvent.Create(CommandStatus.Failure));
                    throw;
                }

                ProgressMessage = String.Format(
                    Resources.AttachDebuggerTestConnectPortMessageFormat,
                    Context.DebuggerPort.Description,
                    Context.PublicIp,
                    Context.DebuggerPort.PortInfo.Port);

                Stopwatch watch = Stopwatch.StartNew();
                while (!CancelToken.IsCancellationRequested &&
                       watch.Elapsed < s_waitConnectionTimeout &&
                       !session.IsStopped)
                {
                    if (await Context.DebuggerPort.ConnectivityTest(CancelToken))
                    {
                        EventsReporterWrapper.ReportEvent(RemoteDebuggerRemoteToolsStartedEvent.Create(
                            CommandStatus.Success, DateTime.Now - startTimestamp));
                        return ListProcessStepViewModel.CreateStep(Context);
                    }
                    await Task.Delay(500);
                }

                EventsReporterWrapper.ReportEvent(
                    RemoteDebuggerRemoteToolsStartedEvent.Create(CommandStatus.Failure));
            }

            return HelpStepViewModel.CreateStep(Context);
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

        private InstallStartRemoteToolStepViewModel(
            InstallStartRemoteToolStepContent content,
            AttachDebuggerContext context)
            : base(context)
        {
            _installer = new RemoteToolInstaller(
                Context.PublicIp,
                Context.Credential.User,
                Context.Credential.Password,
                ToolsPathProvider.GetRemoteDebuggerToolsPath());
            Content = content;
        }
    }
}
