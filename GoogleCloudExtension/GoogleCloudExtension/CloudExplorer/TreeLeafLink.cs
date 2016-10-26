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
using System.Diagnostics;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class represents a node in the cloud explorer that shows a clickable link.
    /// </summary>
    public class TreeLeafLink : TreeHierarchy
    {
        /// <summary>
        /// The <seealso cref="LinkInfo"/> instance that will be shown to the user.
        /// </summary>
        public LinkInfo LinkInfo { get; }

        /// <summary>
        /// The command to execute when the link is pressed.
        /// </summary>
        public ICommand NavigateCommand { get; }

        public TreeLeafLink(LinkInfo linkInfo)
        {
            LinkInfo = linkInfo;
            Caption = LinkInfo.Caption;
            NavigateCommand = new ProtectedCommand(OnNavigateCommand);
        }

        private void OnNavigateCommand()
        {
            Process.Start(LinkInfo.NavigateUrl);
        }
    }
}
