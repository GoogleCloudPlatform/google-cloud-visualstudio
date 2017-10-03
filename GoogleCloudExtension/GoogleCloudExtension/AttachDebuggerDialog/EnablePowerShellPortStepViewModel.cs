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

using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// This step check if the remote powershell HTTPs 5986 port is added to GCE firewall.
    /// If not, ask for confirmation and add the rule.
    /// </summary>
    public class EnablePowerShellPortStepViewModel : EnablePortStepViewModel
    {
        private bool _askedToCheckConnectivityLater = false;

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
        /// This function can be called twice.
        /// 
        /// First time, it simply check connectivity with a short timeout. 
        /// If it does not connect, there are two possibilities. 
        ///   1) Firewall was just enabled, let's check for longer time.
        ///   2) Remote powershell is disabled on target machine etc.
        /// 
        /// Since we are not clear which is the case, we return null. It will then show 
        /// the dialog UI to ask user to choose if he wants to retry testing connectivity.
        /// 
        /// If the user choose yes for retry, this method is called the second time.
        /// The second time, we keep testing connectivity with a longer period of time.
        /// 
        /// Both times, 
        /// if remote powershell can be connected, go to install, start remote debugger step.
        /// 
        /// If it is determined it won't connect successfully, 
        /// go to a help page with a link to our documentation.
        /// </summary>
        protected override async Task<IAttachDebuggerStep> GetNextStep()
        {
            SetStage(Stage.CheckingConnectivity);
            var port = Context.RemotePowerShellPort;
            int waitTime = port.WaitForFirewallRuleTimeInSeconds();
            bool connected = waitTime > 0 && _askedToCheckConnectivityLater ?
                // This is the second time, check connectivity with a longer wait time.
                await ConnectivityTestUntillTimeout(waitTime) :
                // This is the first time, we don't check "waitTime", test connectivity anyhow.
                await port.ConnectivityTest(CancelToken);
            if (connected)
            {
                return InstallStartRemoteToolStepViewModel.CreateStep(Context);
            }
            // else: not connected

            // If this is first time call, then check if firewall was just enabled.
            if (!_askedToCheckConnectivityLater && port.WaitForFirewallRuleTimeInSeconds() > 0)
            {
                SetStage(Stage.AskToCheckConnectivityLater);
                _askedToCheckConnectivityLater = true;
                return null;
            }
            else
            {
                return HelpStepViewModel.CreateStep(Context);
            }
        }

        private async Task<bool> ConnectivityTestUntillTimeout(int waitTime)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalSeconds < waitTime && !CancelToken.IsCancellationRequested)
            {
                if (await Context.RemotePowerShellPort.ConnectivityTest(CancelToken))
                {
                    return true;
                }
            }
            return false;
        }

        private EnablePowerShellPortStepViewModel(EnablePortStepContent content, AttachDebuggerContext context)
            : base(content, context.RemotePowerShellPort, context)
        { }
    }
}
