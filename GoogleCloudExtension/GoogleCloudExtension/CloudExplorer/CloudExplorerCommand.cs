// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// Command handler for the CloudExplorerCommand that opens the Tool Window.
    /// </summary>
    internal sealed class CloudExplorerCommand
    {
        private const string ShowCloudExplorerToolWindowCommand = nameof(ShowCloudExplorerToolWindowCommand);

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("266caee5-128b-4fdd-a22d-d7a7c8017794");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CloudExplorerCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new CloudExplorerCommand(package);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudExplorerCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CloudExplorerCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            ExtensionAnalytics.ReportCommand(
                ShowCloudExplorerToolWindowCommand,
                CommandInvocationSource.ToolsMenu,
                () =>
                {
                    if (AccountsManager.CurrentAccount == null)
                    {
                        Debug.WriteLine("Attempted to open cloud explorer without credentials.");
                        UserPromptUtils.OkPrompt("Plase login beore using this tool.", "Need credentials.");
                        return;
                    }

                    // Get the instance number 0 of this tool window. This window is single instance so this instance
                    // is actually the only one.
                    // The last flag is set to true so that if the tool window does not exists it will be created.
                    ToolWindowPane window = _package.FindToolWindow(typeof(CloudExplorerToolWindow), 0, true);
                    if (window?.Frame == null)
                    {
                        throw new NotSupportedException("Cannot create tool window");
                    }

                    IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                });
        }
    }
}
