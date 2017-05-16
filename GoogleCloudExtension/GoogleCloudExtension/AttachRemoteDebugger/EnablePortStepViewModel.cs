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

using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using static GoogleCloudExtension.AttachRemoteDebugger.AttachDebuggerContext;

namespace GoogleCloudExtension.AttachRemoteDebugger
{
    /// <summary>
    /// Enable debugger port by adding a GCE firewall rule.
    /// </summary>
    public abstract class EnablePortStepViewModel : AttachDebuggerStepBase
    {
        // TODO: update the link when we have the doc ready.
        private const string EnablePortHelpLink = "https://cloud.google.com/tools/visual-studio/docs/how-to";

        private bool _portEnabled;
        private string _progressMessage;
        private bool _askingToConfirm;
        private bool _askingToTestConnectivityLater;
        private PortInfo _portInfo;
        private Stage _stage;

        /// <summary>
        /// Show the message if the port is not enabled.
        /// </summary>
        public string PortDisabledMessage => string.Format(Resources.AttachDebuggerPortDisabledMessageFormat, _portInfo.Port);

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
            protected set { SetValueAndRaise(ref _askingToConfirm, value); }
        }

        /// <summary>
        /// Get if it is asking user to try connect later
        /// </summary>
        public bool IsAskingToTestConnectivityLater
        {
            get { return _askingToTestConnectivityLater; }
            protected set { SetValueAndRaise(ref _askingToTestConnectivityLater, value); }
        }

        /// <summary>
        /// The message when operation is in progress.
        /// </summary>
        public string ProgressMessage
        {
            get { return _progressMessage; }
            set { SetValueAndRaise(ref _progressMessage, value); }
        }

        /// <summary>
        /// Initializes the <seealso cref="EnableDebuggerPortViewModel"/>
        /// </summary>
        /// <param name="content">The associated user control.</param>
        /// <param name="portInfo">The port to open.</param>
        public EnablePortStepViewModel(UserControl content, PortInfo portInfo)
        {
            Content = content;
            _portInfo = portInfo;
            SetStage(Stage.Init);
            EnablePortHelpLinkCommand = new ProtectedCommand(() => Process.Start(EnablePortHelpLink));
        }

        protected abstract Task<IAttachDebuggerStep> GetNextStep();

        #region Implement interface IAttachDebuggerStep
        public override ContentControl Content { get; }

        public override async Task<IAttachDebuggerStep> OnStart()
        {
            SetStage(Stage.CheckingFirewallRule);
            if (await IsPortEnabled(_portInfo))
            {
                return await GetNextStep();
            }
            else
            {
                SetStage(Stage.AskingPermitToAddRule);
                return null;
            }
        }

        public override Task<IAttachDebuggerStep> OnCancelCommand()
        {
            Context.DialogWindow.Close();
            return Task.FromResult<IAttachDebuggerStep>(null);    // TODO:  return help page as next step.
        }

        public override async Task<IAttachDebuggerStep> OnOKCommand()
        {
            SetStage(Stage.AddingFirewallRule);
            if (!_portEnabled)
            {
                SetStage(Stage.AddingFirewallRule);
                await Context.EnablePort(_portInfo);
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
                        Resources.AttachDebuggerTestConnectPortMessageFormat, Context.PublicIp, _portInfo.Port);
                    IsCancelButtonEnabled = true;
                    break;
                case Stage.AskToCheckConnectivityLater:
                    IsCancelButtonEnabled = true;
                    IsOKButtonEnabled = true;
                    IsAskingToTestConnectivityLater = true;
                    break;
            }
        }

        protected enum Stage
        {
            Init = -1,
            CheckingFirewallRule,
            AskingPermitToAddRule,
            AddingFirewallRule,
            CheckingConnectivity,
            AskToCheckConnectivityLater,
        }

        /// <summary>
        /// Check if GCE firewall rules include a rule that enables the port to target GCE VM.
        /// A firewall rule contains tag, 
        /// if the GCE instance also has the tag, the rule is applied to the GCE instance.
        /// </summary>
        /// <param name="portInfo">A <seealso cref="PortInfo"/> object that represents the port.</param>
        /// <param name="gceInstance">GCE Instance</param>
        private async Task<bool> IsPortEnabled(PortInfo portInfo)
        {
            string portTag = portInfo.GetTag(Context.GceInstance);

            // If the instance does not contain the tag, the firewall rule is not set.
            if (Context.GceInstance.Tags?.Items?.Contains(portTag) == true)
            {
                var rules = await Context.DataSource.GetFirewallListAsync();
                foreach (var rule in rules)
                {
                    // Left oprand is nullable bool.  null == false is false. null == true is false.
                    if (!(rule.TargetTags?.Contains(portTag) == true))
                    {
                        continue;   // Skip, rules does not contain the tag.
                    }
                    foreach (var allowed in rule.Allowed)
                    {
                        if (allowed.IPProtocol == "tcp" && allowed.Ports.Any(y => y == portInfo.Port.ToString()))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
