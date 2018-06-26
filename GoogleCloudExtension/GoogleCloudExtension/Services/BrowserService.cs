// Copyright 2018 Google Inc. All Rights Reserved.
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

using System.ComponentModel.Composition;
using System.Diagnostics;

namespace GoogleCloudExtension.Services
{
    /// <summary>
    /// Service for opening a browser window.
    /// </summary>
    [Export(typeof(IBrowserService))]
    public class BrowserService : IBrowserService
    {
        /// <summary>
        /// Opens the default system browser to the given url.
        /// </summary>
        /// <param name="url">The url to open the browser at.</param>
        public void OpenBrowser(string url) => Process.Start(url);
    }
}
