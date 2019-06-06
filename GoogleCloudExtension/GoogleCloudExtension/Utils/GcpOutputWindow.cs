// Copyright 2016 Google Inc. All Rights Reserved.
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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers to manage and add lines of text to the GCP window.
    /// </summary>
    [Export(typeof(IGcpOutputWindow))]
    internal class GcpOutputWindow : IGcpOutputWindow
    {
        private const string WindowTitle = "Google Cloud Tools";
        private static readonly Guid s_windowGuid = new Guid("E701CE22-DDEA-418A-9E66-C5A4F3891958");

        public static IGcpOutputWindow Default => GoogleCloudExtensionPackage.Instance.GcpOutputWindow;

        private static JoinableTaskFactory JoinableTaskFactory =>
            GoogleCloudExtensionPackage.Instance.JoinableTaskFactory;

        private readonly AsyncLazy<IVsOutputWindowPane> _outputWindowPane =
            new AsyncLazy<IVsOutputWindowPane>(GetOutputWindowPaneAsync, JoinableTaskFactory);

        private static async Task<IVsOutputWindowPane> GetOutputWindowPaneAsync()
        {
            IVsOutputWindow outputWindow =
                await GoogleCloudExtensionPackage.Instance.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>();
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(s_windowGuid, WindowTitle, 1, 1));

            ErrorHandler.ThrowOnFailure(outputWindow.GetPane(s_windowGuid, out IVsOutputWindowPane outputWindowPane));
            return outputWindowPane;
        }

        /// <summary>
        /// Outputs a line to the GCP output window pane.
        /// </summary>
        /// <param name="str">The line of text to output.</param>
        public async Task OutputLineAsync(string str)
        {
            IVsOutputWindowPane outputWindowPane = await _outputWindowPane.GetValueAsync();
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            outputWindowPane.OutputString(str);
            outputWindowPane.OutputString("\n");
        }

        /// <summary>
        /// Activates the GCP output window pane, making sure it is visible for the user.
        /// </summary>
        public async Task ActivateAsync()
        {
            IVsOutputWindowPane outputWindowPane = await _outputWindowPane.GetValueAsync();
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            outputWindowPane.Activate();
        }

        /// <summary>
        /// Clears all of the content from the GCP window pane.
        /// </summary>
        public async Task ClearAsync()
        {
            IVsOutputWindowPane outputWindowPane = await _outputWindowPane.GetValueAsync();
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            outputWindowPane.Clear();
        }
    }
}
