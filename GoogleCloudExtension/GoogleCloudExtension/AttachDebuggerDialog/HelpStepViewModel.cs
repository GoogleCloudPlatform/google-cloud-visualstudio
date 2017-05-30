﻿// Copyright 2017 Google Inc. All Rights Reserved.
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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// If attaching debugger does not succeed by any means,
    /// asks user to visit online documentation.
    /// </summary>
    public class HelpStepViewModel : AttachDebuggerStepBase
    {
        // TODO: update the link when we have the doc ready.
        private const string HelpLink = "https://cloud.google.com/tools/visual-studio/docs/how-to";

        /// <summary>
        /// The command to open the enable port help hyperlink.
        /// </summary>
        public ProtectedCommand HelpLinkCommand { get; }

        public HelpStepViewModel(
            HelpStepContent content,
            AttachDebuggerContext context)
            : base(context)
        {
            Content = content;
            HelpLinkCommand = new ProtectedCommand(() => Process.Start(HelpLink));
        }

        #region Implement interface IAttachDebuggerStep
        public override ContentControl Content { get; }

        public override Task<IAttachDebuggerStep> OnOkCommandAsync() => Task.FromResult(base.OnCancelCommand());

        public override Task<IAttachDebuggerStep> OnStartAsync()
        {
            IsCancelButtonEnabled = true;
            IsOKButtonEnabled = true;
            return Task.FromResult<IAttachDebuggerStep>(null);
        }
        #endregion

        /// <summary>
        /// Creates the step that installs and starts debugger remote tool.
        /// </summary>
        public static HelpStepViewModel CreateStep(AttachDebuggerContext context)
        {
            var content = new HelpStepContent();
            var step = new HelpStepViewModel(content, context);
            content.DataContext = step;
            return step;
        }
    }
}
