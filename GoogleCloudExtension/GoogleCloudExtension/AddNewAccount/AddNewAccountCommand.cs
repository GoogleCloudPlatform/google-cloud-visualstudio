// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace GoogleCloudExtension.AddNewAccount
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddNewAccountCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4180;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("266caee5-128b-4fdd-a22d-d7a7c8017794");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewAccountCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private AddNewAccountCommand(Package package)
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
                var menuItem = new MenuCommand(this.AddNewAccountHandler, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AddNewAccountCommand Instance { get; private set; }

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
            Instance = new AddNewAccountCommand(package);
        }

        /// <summary>
        /// This is the handler that executes when the user presses on the command button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void AddNewAccountHandler(object sender, EventArgs e)
        {
            AppEngineOutputWindow.Clear();
            AppEngineOutputWindow.Activate();
            try
            {
                AppEngineOutputWindow.OutputLine("Activating browser.");
                await GCloudWrapper.Instance.AddCredentialsAsync(AppEngineOutputWindow.OutputLine);
                AppEngineOutputWindow.OutputLine("Done adding account.");
            }
            catch (Exception ex)
            {
                AppEngineOutputWindow.OutputLine(ex.Message);
            }
        }
    }
}
