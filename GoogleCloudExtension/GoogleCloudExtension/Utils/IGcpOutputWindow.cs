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

using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Service interface for controlling the Google Cloud Tools output window pane.
    /// </summary>
    public interface IGcpOutputWindow
    {
        /// <summary>
        /// Outputs debug information to the Visual Studio output window as well as to the
        /// debug output.
        /// </summary>
        void OutputDebugLine(string str);

        /// <summary>
        /// Outputs a line to the GCP output window pane.
        /// </summary>
        /// <param name="str">The line of text to output.</param>
        void OutputLine(string str);

        /// <summary>
        /// Outputs a line to the GCP output window pane.
        /// </summary>
        /// <param name="str">The line of text to output.</param>
        Task OutputLineAsync(string str);

        /// <summary>
        /// Activates the GCP output window pane, making sure it is visible for the user.
        /// </summary>
        Task ActivateAsync();

        /// <summary>
        /// Clears all of the content from the GCP window pane.
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Outputs a line to the GCP output window pane.
        /// </summary>
        /// <param name="line">The line of text to output.</param>
        /// <param name="sourceStream">The source stream of the output (stderr or stdout). This value is ignored.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the line has been output to the Gcp Output window.
        /// </returns>
        Task OutputLineAsync(string line, OutputStream sourceStream);
    }
}