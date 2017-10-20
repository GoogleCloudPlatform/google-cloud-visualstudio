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

using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Enable debugger port by adding a GCE firewall rule.
    /// </summary>
    public abstract class EnablePortStepViewModel : AttachDebuggerStepBase
    {
        private const string EnablePortHelpLink = "https://cloud.google.com/tools/visual-studio/docs/remote-debugging#open_firewall_port";

        private bool _portEnabled;
        private string _progressMessage;
        private bool _askingToConfirm;
        private bool _askingToTestConnectivityLater;
        private readonly AttachDebuggerFirewallPort _port;

        /// <summary>
        /// Show the message if the port is not enabled.
        /// </summary>
        public string PortDisabledMessage => string.Format(
            Resources.AttachDebuggerPortDisabledMessageFormat,
            _port.PortInfo.Port,
            _port.Description);

        /// <summary>
        /// The command to open the enable port help hyperlink.
        /// </summary>
        public ProtectedCommand EnablePortHelpLinkCommand { get; }

        /// <summary>
        /// Get if it is asking for permission to open the port.
        /// </summary>
        public bool IsAskingToConfirm
        {
            get { return _askingToConfirm; }
            private set { SetValueAndRaise(ref _askingToConfirm, value); }
        }

        /// <summary>
        /// Get if it is asking user to try connect later
        /// </summary>
        public bool IsAskingToTestConnectivityLater
        {
            get { return _askingToTestConnectivityLater; }
            private set { SetValueAndRaise(ref _askingToTestConnectivityLater, value); }
        }

        /// <summary>
        /// The message when operation is in progress.
        /// </summary>
        public string ProgressMessage
        {
            get { return _progressMessage; }
            private set { SetValueAndRaise(ref _progressMessage, value); }
        }

        /// <summary>
        /// Initializes an instance of the <seealso cref="EnablePortStepViewModel"/> class.
        /// </summary>
        /// <param name="content">The associated user control.</param>
        /// <param name="port">The port to open.</param>
        /// <param name="context">The <seealso cref="AttachDebuggerContext"/> object.</param>
        public EnablePortStepViewModel(
            EnablePortStepContent content,
            AttachDebuggerFirewallPort port,
            AttachDebuggerContext context)
            : base(context)
        {
            Content = content;
            _port = port;
            SetStage(Stage.Init);
            EnablePortHelpLinkCommand = new ProtectedCommand(() => Process.Start(EnablePortHelpLink));
        }

        protected abstract Task<IAttachDebuggerStep> GetNextStep();

        #region Implement interface IAttachDebuggerStep
        public override ContentControl Content { get; }

        public override async Task<IAttachDebuggerStep> OnStartAsync()
        {
            SetStage(Stage.CheckingFirewallRule);
            if (await _port.IsPortEnabled())
            {
                return await GetNextStep();
            }
            else
            {
                SetStage(Stage.AskingPermitToAddRule);
                return null;
            }
        }

        public override async Task<IAttachDebuggerStep> OnOkCommandAsync()
        {
            SetStage(Stage.AddingFirewallRule);
            if (!_portEnabled)
            {
                SetStage(Stage.AddingFirewallRule);
                await _port.EnablePort();
                _portEnabled = true;
            }
            return await GetNextStep();
        }
        #endregion

        protected void SetStage(Stage newStage)
        {
            ProgressMessage = null;
            IsAskingToConfirm = false;
            IsAskingToTestConnectivityLater = false;
            IsOKButtonEnabled = false;
            IsCancelButtonEnabled = false;
            switch (newStage)
            {
                case Stage.Init:
                    break;
                case Stage.CheckingFirewallRule:
                    ProgressMessage = Resources.AttachDebuggerCheckFirewallRuleMessage;
                    IsCancelButtonEnabled = true;
                    break;
                case Stage.AskingPermitToAddRule:
                    IsAskingToConfirm = true;
                    IsOKButtonEnabled = true;
                    IsCancelButtonEnabled = true;
                    break;
                case Stage.AddingFirewallRule:
                    ProgressMessage = Resources.AttachDebuggerAddingFirewallRuleMessage;
                    IsOKButtonEnabled = true;
                    IsCancelButtonEnabled = true;
                    break;
                case Stage.CheckingConnectivity:
                    ProgressMessage = String.Format(
                        Resources.AttachDebuggerTestConnectPortMessageFormat,
                        _port.Description, Context.PublicIp, _port.PortInfo.Port);
                    IsCancelButtonEnabled = true;
                    break;
                case Stage.AskToCheckConnectivityLater:
                    IsCancelButtonEnabled = true;
                    IsOKButtonEnabled = true;
                    IsAskingToTestConnectivityLater = true;
                    break;
            }
        }

        /// <summary>
        /// Define operation stages inside this class. 
        /// This is to help properly display progress message.
        /// </summary>
        protected enum Stage
        {
            /// <summary>
            /// Init state
            /// </summary>
            Init = 0,

            /// <summary>
            /// Check if there is firewall rule for the port.
            /// </summary>
            CheckingFirewallRule,

            /// <summary>
            /// Asking user to confirm to open the port.
            /// </summary>
            AskingPermitToAddRule,

            /// <summary>
            /// Adding firewall rule.
            /// </summary>
            AddingFirewallRule,

            /// <summary>
            /// Test if this machine can "telnet" to the remote ip:port.
            /// </summary>
            CheckingConnectivity,

            /// <summary>
            /// If firewall is just added, ask user to retry testing connectivity.
            /// </summary>
            AskToCheckConnectivityLater,
        }
    }
}
