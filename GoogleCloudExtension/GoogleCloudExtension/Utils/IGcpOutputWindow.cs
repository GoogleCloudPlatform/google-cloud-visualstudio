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
    }
}