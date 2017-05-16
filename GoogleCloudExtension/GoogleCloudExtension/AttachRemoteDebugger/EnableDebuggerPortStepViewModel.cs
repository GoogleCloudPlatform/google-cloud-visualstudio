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
using System.Threading.Tasks;
using System.Windows.Controls;
using static GoogleCloudExtension.AttachRemoteDebugger.AttachDebuggerContext;
using static GoogleCloudExtension.VsVersion.VsVersionUtils;

namespace GoogleCloudExtension.AttachRemoteDebugger
{
    public class EnableDebuggerPortStepViewModel : EnablePortStepViewModel
    {
        /// <summary>
        /// The <seealso cref="PortInfo"/> for Visual Studio Remote Debugger tool port.
        /// </summary>
        public static readonly PortInfo s_DebuggerPortInfo =
            new PortInfo("VSRemoteDebugger", RemoteDebuggerPort, Resources.PortManagerRemoteDebuggerDescription);

        public EnableDebuggerPortStepViewModel(UserControl content)
            : base(content, s_DebuggerPortInfo)
        { }

        /// <summary>
        /// Create the the step that enables Visual Studio remote debugging tool port.
        /// </summary>
        public static IAttachDebuggerStep CreateStep() => 
            Context.CreateStep<EnableDebuggerPortStepViewModel, EnablePortStepContent>();

        protected override async Task<IAttachDebuggerStep> GetNextStep()
        {
            SetStage(Stage.CheckingConnectivity);
            if (!(await Context.ConnectivityTest(RemoteDebuggerPort)))
            {
                return EnablePowerShellPortStepViewModel.CreateStep();
            }
            else
            {
                return null;    // TODO: I'll add ListProcess step.
            }
        }
    }
}
