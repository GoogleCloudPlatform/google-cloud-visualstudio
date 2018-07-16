// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal static class ErrorReportingToolWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4236;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a7435138-27e2-410c-9d28-dffc5aa3fe80");

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="token"></param>
        public static async Task InitializeAsync(IGoogleCloudExtensionPackage package, CancellationToken token)
        {
            package.ThrowIfNull(nameof(package));

            if (await package.GetServiceAsync(typeof(IMenuCommandService)) is IMenuCommandService commandService)
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(
                    async (sender, e) =>
                    {
                        await ToolWindowCommandUtils.ShowToolWindowAsync<ErrorReportingToolWindow>();
                        EventsReporterWrapper.ReportEvent(ErrorsViewerOpenEvent.Create());
                    },
                    menuCommandID);
                menuItem.BeforeQueryStatus += ToolWindowCommandUtils.EnableMenuItemOnValidProjectId;
                commandService.AddCommand(menuItem);
            }
        }
    }
}
