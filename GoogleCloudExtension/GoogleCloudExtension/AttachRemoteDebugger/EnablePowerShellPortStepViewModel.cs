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
using static GoogleCloudExtension.AttachRemoteDebugger.AttachDebuggerContext;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachRemoteDebugger
{
    public class EnablePowerShellPortStepViewModel : EnablePortStepViewModel
    {
        /// <summary>
        /// The <seealso cref="PortInfo"/> for remote PowerShell HTTPs port.
        /// </summary>
        public static readonly PortInfo s_RemotePowerShellPortInfo =
            new PortInfo("HTTPSRemotePowerShell", 5986, description: Resources.PortManagerRemotePowershellDescription);


        public EnablePowerShellPortStepViewModel(UserControl content)
            : base(content, s_RemotePowerShellPortInfo)
        { }

        /// <summary>
        /// Create the the step that enables Visual Studio remote debugging tool port.
        /// </summary>
        public static IAttachDebuggerStep CreateStep() => 
            Context.CreateStep<EnablePowerShellPortStepViewModel, EnablePortStepContent>();

        protected override async Task<IAttachDebuggerStep> GetNextStep()
        {
            SetStage(Stage.CheckingConnectivity);
            if (await Context.ConnectivityTest(s_RemotePowerShellPortInfo.Port))
            {
                return null;    // TODO: I'll add install debugger tool step.
            }
            else
            {
                if (Context.ShouldWaitForFirewallRule(s_RemotePowerShellPortInfo.Port))
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
