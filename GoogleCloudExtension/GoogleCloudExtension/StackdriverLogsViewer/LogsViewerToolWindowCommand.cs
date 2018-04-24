﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class LogsViewerToolWindowCommand
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly IGoogleCloudExtensionPackage _package;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4235;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a7435138-27e2-410c-9d28-dffc5aa3fe80");

        public static readonly CommandID MenuCommandID = new CommandID(CommandSet, CommandId);

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsViewerToolWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private LogsViewerToolWindowCommand(IGoogleCloudExtensionPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            if (commandService != null)
            {
                var menuItem = new OleMenuCommand(
                    (sender, e) => ToolWindowCommandUtils.AddToolWindow<LogsViewerToolWindow>(), MenuCommandID);
                menuItem.BeforeQueryStatus += ToolWindowCommandUtils.EnableMenuItemOnValidProjectId;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static LogsViewerToolWindowCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(IGoogleCloudExtensionPackage package)
        {
            Instance = new LogsViewerToolWindowCommand(package);
        }
    }
}
