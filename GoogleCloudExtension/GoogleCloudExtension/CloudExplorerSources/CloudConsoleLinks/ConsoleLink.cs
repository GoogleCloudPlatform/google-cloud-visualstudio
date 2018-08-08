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

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks
{
    /// <summary>
    /// This class represents a node in the cloud explorer that shows a clickable link.
    /// </summary>
    public class ConsoleLink : TreeLeaf
    {
        private readonly LinkInfo _linkFormatInfo;
        private readonly ICloudSourceContext _context;
        private readonly Func<string, Process> _startProcess;

        /// <summary>
        /// The command to execute when the link is pressed.
        /// </summary>
        public ProtectedCommand NavigateCommand { get; }

        /// <summary>
        /// Creates a new Console Link tree leaf node.
        /// </summary>
        /// <param name="linkFormatInfo">
        /// The link info with the caption and the <see cref="string.Format(string,object[])"/> ready url format.
        /// </param>
        /// <param name="context">The <see cref="ICloudSourceContext"/>.</param>
        public ConsoleLink(LinkInfo linkFormatInfo, ICloudSourceContext context) : this(
            linkFormatInfo, context, Process.Start)
        { }

        /// <summary>
        /// Internal constructor for testing.
        /// </summary>
        /// <param name="linkFormatInfo">
        /// The link info with the caption and the <see cref="string.Format(string,object[])"/> ready url format.
        /// </param>
        /// <param name="context">The <see cref="ICloudSourceContext"/>.</param>
        /// <param name="startProcess">
        /// Dependency injecion of the static function <see cref="Process.Start(string)"/>.
        /// </param>
        internal ConsoleLink(LinkInfo linkFormatInfo, ICloudSourceContext context, Func<string, Process> startProcess)
        {
            _startProcess = startProcess;
            _context = context;
            _linkFormatInfo = linkFormatInfo;
            Caption = _linkFormatInfo.Caption;
            NavigateCommand = new ProtectedCommand(OnNavigateCommand);
        }

        private void OnNavigateCommand()
        {
            _startProcess(string.Format(_linkFormatInfo.NavigateUrl, _context.CurrentProject?.ProjectId));
        }
    }
}
