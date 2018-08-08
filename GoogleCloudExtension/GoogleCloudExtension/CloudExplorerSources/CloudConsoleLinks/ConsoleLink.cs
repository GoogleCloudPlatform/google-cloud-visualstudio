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
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks
{
    /// <summary>
    /// This class represents a node in the cloud explorer that shows a clickable link.
    /// </summary>
    public class ConsoleLink : TreeLeaf
    {
        private readonly LinkInfo _linkFormatInfo;
        private readonly ICloudSourceContext _context;
        private readonly Lazy<IBrowserService> _browserService =
            GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IBrowserService>();

        /// <summary>
        /// The command to execute when the link is pressed.
        /// </summary>
        public ProtectedCommand NavigateCommand { get; }

        /// <summary>
        /// The link info for the help link, if any.
        /// </summary>
        public LinkInfo InfoLinkInfo { get; }

        /// <summary>
        /// The command to navigate to the info link.
        /// </summary>
        public ProtectedCommand NavigateInfoCommand { get; }

        private IBrowserService BrowserService => _browserService.Value;

        /// <summary>
        /// Creates a new Console Link tree leaf node.
        /// </summary>
        /// <param name="context">The <see cref="ICloudSourceContext"/>.</param>
        /// <param name="linkFormatInfo">
        /// The link info with the caption and the <see cref="string.Format(string,object[])"/> ready url format.
        /// </param>
        /// <param name="infoLinkInfo">The link info for the help section of the console link.</param>
        public ConsoleLink(ICloudSourceContext context, LinkInfo linkFormatInfo, LinkInfo infoLinkInfo) : this(context, linkFormatInfo)
        {
            InfoLinkInfo = infoLinkInfo;
            NavigateInfoCommand.CanExecuteCommand = true;
        }

        /// <summary>
        /// Creates a new Console Link tree leaf node.
        /// </summary>
        /// <param name="context">The <see cref="ICloudSourceContext"/>.</param>
        /// <param name="linkFormatInfo">
        /// The link info with the caption and the <see cref="string.Format(string,object[])"/> ready url format.
        /// </param>
        public ConsoleLink(ICloudSourceContext context, LinkInfo linkFormatInfo)
        {
            _context = context;
            _linkFormatInfo = linkFormatInfo;
            Caption = _linkFormatInfo.Caption;
            NavigateCommand = new ProtectedCommand(OnNavigateCommand);
            NavigateInfoCommand = new ProtectedCommand(OnNavigateHelpCommand, false);
        }

        private void OnNavigateCommand() => BrowserService.OpenBrowser(
            string.Format(_linkFormatInfo.NavigateUrl, _context.CurrentProject?.ProjectId));

        private void OnNavigateHelpCommand() => BrowserService.OpenBrowser(InfoLinkInfo.NavigateUrl);
    }
}
