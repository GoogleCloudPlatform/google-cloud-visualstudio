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

using System.Threading.Tasks;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// This step check if the Visual Studio Remote Debugger tool port is added to GCE firewall.
    /// If not, ask for confirmation and add the rule.
    /// </summary>
    public class EnableDebuggerPortStepViewModel : EnablePortStepViewModel
    {
        /// <summary>
        /// Create the the step that enables Visual Studio remote debugging tool port.
        /// </summary>
        public static EnableDebuggerPortStepViewModel CreateStep(AttachDebuggerContext context)
        {
            var content = new EnablePortStepContent();
            var step = new EnableDebuggerPortStepViewModel(content, context);
            content.DataContext = step;
            return step;
        }

        /// <summary>
        /// If it connects to remote debugger tool successfully, go to ListProcess step.
        /// Otherwise go to enalbe remote powershell port step that later it goes to install, start remote tool step.
        /// </summary>
        protected override async Task<IAttachDebuggerStep> GetNextStep()
        {
            SetStage(Stage.CheckingConnectivity);
            if (!(await Context.DebuggerPort.ConnectivityTest()))
            {
                return EnablePowerShellPortStepViewModel.CreateStep(Context);
            }
            else
            {
                return ListProcessStepViewModel.CreateStep(Context);
            }
        }

        private EnableDebuggerPortStepViewModel(EnablePortStepContent content, AttachDebuggerContext context)
            : base(content, context.DebuggerPort, context)
        { }
    }
}
