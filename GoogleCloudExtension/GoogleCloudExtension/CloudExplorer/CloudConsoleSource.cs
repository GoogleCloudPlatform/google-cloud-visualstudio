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

using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class represents the node in the Cloud Explorer that points the users towards
    /// the Cloud Console for more services.
    /// </summary>
    public class CloudConsoleSource : ICloudExplorerSource
    {
        private static readonly LinkInfo s_consoleLink = new LinkInfo(
            link: "https://console.cloud.google.com",
            caption: Resources.CloudExplorerConsoleLinkCaption);

        public CloudConsoleSource()
        {
            Root = new TreeLeafLink(s_consoleLink);
        }

        public IEnumerable<ButtonDefinition> Buttons => Enumerable.Empty<ButtonDefinition>();

        public TreeHierarchy Root { get; }

        public void InvalidateProjectOrAccount()
        { }

        public void Refresh()
        { }
    }
}
