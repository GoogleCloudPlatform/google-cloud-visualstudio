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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Composition;
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

        private readonly Lazy<IVsOutputWindowPane> _outputWindowPane = new Lazy<IVsOutputWindowPane>(() =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            outputWindow?.CreatePane(s_windowGuid, WindowTitle, 1, 1);

            IVsOutputWindowPane outputWindowPane = null;
            outputWindow?.GetPane(s_windowGuid, out outputWindowPane);

            return outputWindowPane;
        });

        /// <summary>
        /// Outputs a line to the GCP output window pane.
        /// </summary>
        /// <param name="str">The line of text to output.</param>
        public void OutputLine(string str)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputWindowPane.Value?.OutputString(str);
            _outputWindowPane.Value?.OutputString("\n");
        }

        /// <summary>
        /// Outputs a line to the GCP output window pane.
        /// </summary>
        /// <param name="str">The line of text to output.</param>
        public async Task OutputLineAsync(string str)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _outputWindowPane.Value?.OutputString(str);
            _outputWindowPane.Value?.OutputString("\n");
        }

        /// <summary>
        /// Outputs a line to the GCP output window pane.
        /// </summary>
        /// <param name="line">The line of text to output.</param>
        /// <param name="sourceStream">The source stream of the output (stderr or stdout). This value is ignored.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the line has been output to the Gcp Output window.
        /// </returns>
        public Task OutputLineAsync(string line, OutputStream sourceStream) => OutputLineAsync(line);

        /// <summary>
        /// Outputs the line from the given OutputEventArg to the GCP output window pane.
        /// </summary>
        /// <param name="sender">The sender this event comes from.</param>
        /// <param name="args">The <see cref="OutputHandlerEventArgs"/> for the event.</param>
        public void OutputLine(object sender, OutputHandlerEventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OutputLine(args.Line);
        }

        /// <summary>
        /// Outputs debug information to the Visual Studio output window as well as to the
        /// debug output.
        /// </summary>
        public void OutputDebugLine(string str)
        {
#if DEBUG
            ThreadHelper.ThrowIfNotOnUIThread();
            OutputLine(str);
#endif
        }

        /// <summary>
        /// Activates the GCP output window pane, making sure it is visible for the user.
        /// </summary>
        public void Activate()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputWindowPane.Value?.Activate();
        }

        /// <summary>
        /// Activates the GCP output window pane, making sure it is visible for the user.
        /// </summary>
        public async Task ActivateAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _outputWindowPane.Value?.Activate();
        }

        /// <summary>
        /// Clears all of the content from the GCP window pane.
        /// </summary>
        public void Clear()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputWindowPane.Value?.Clear();
        }

        /// <summary>
        /// Clears all of the content from the GCP window pane.
        /// </summary>
        public async Task ClearAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _outputWindowPane.Value?.Clear();
        }
    }
}
