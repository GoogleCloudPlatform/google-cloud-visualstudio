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
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers to manage and add lines of text to the GCP window.
    /// </summary>
    internal static class GcpOutputWindow
    {
        private const string WindowTitle = "Google Cloud Tools";

        private static readonly Guid s_windowGuid = new Guid("E701CE22-DDEA-418A-9E66-C5A4F3891958");
        private static readonly Lazy<IVsOutputWindowPane> s_outputWindowPane = new Lazy<IVsOutputWindowPane>(() =>
        {
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            outputWindow.CreatePane(s_windowGuid, WindowTitle, 1, 1);

            IVsOutputWindowPane outputWindowPane = null;
            outputWindow.GetPane(s_windowGuid, out outputWindowPane);

            return outputWindowPane;
        });

        /// <summary>
        /// Outputs a line to the GCP output window.
        /// </summary>
        /// <param name="str">The line of text to output.</param>
        public static void OutputLine(string str)
        {
            s_outputWindowPane.Value.OutputString(str);
            s_outputWindowPane.Value.OutputString("\n");
        }

        /// <summary>
        /// Outputs debug information to the Visual Studio output window as well as to the
        /// debug output.
        /// </summary>
        [Conditional("DEBUG")]
        public static void OutputDebugLine(string str)
        {
            OutputLine(str);
            Debug.WriteLine(str);
        }

        /// <summary>
        /// Activates the GCP output window, making sure it is visible for the user.
        /// </summary>
        public static void Activate()
        {
            s_outputWindowPane.Value.Activate();
        }

        /// <summary>
        /// Clears all of the content from the GCP window.
        /// </summary>
        public static void Clear()
        {
            s_outputWindowPane.Value.Clear();
        }
    }
}
