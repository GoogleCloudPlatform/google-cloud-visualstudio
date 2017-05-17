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
using static GoogleCloudExtension.AttachDebuggerDialog.AttachDebuggerContext;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// This step check if the remote powershell HTTPs 5986 port is added to GCE firewall.
    /// If not, ask for confirmation and add the rule.
    /// </summary>
    public class EnablePowerShellPortStepViewModel : EnablePortStepViewModel
    {
        public EnablePowerShellPortStepViewModel(EnablePortStepContent content, AttachDebuggerContext context)
            : base(content, context.RemotePowerShellPort, context)
        { }

        /// <summary>
        /// Create the the step that enables Visual Studio remote debugging tool port.
        /// </summary>
        public static EnablePowerShellPortStepViewModel CreateStep(AttachDebuggerContext context)
        {
            var content = new EnablePortStepContent();
            var step = new EnablePowerShellPortStepViewModel(content, context);
            content.DataContext = step;
            return step;
        }

        /// <summary>
        /// If remote powershell can be connected, go to install, start remote debugger step.
        /// If the firewall rule was just added, ask to wait and retry.
        /// All other cases, go to a help page with a link to our documentation.
        /// </summary>
        protected override async Task<IAttachDebuggerStep> GetNextStep()
        {
            SetStage(Stage.CheckingConnectivity);
            if (await Context.RemotePowerShellPort.ConnectivityTest())
            {
                return null;    // TODO: I'll add install debugger tool step.
            }
            else
            {
                if (Context.RemotePowerShellPort.ShouldWaitForFirewallRule())
                {
                    SetStage(Stage.AskToCheckConnectivityLater);
                    return null;
                }
                else
                {
                    Context.DialogWindow.Close();
                    return null;    // TODO: I'll add a help page later.
                }
            }
        }
    }
}
