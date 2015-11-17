// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DeploymentDialog;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace GoogleCloudExtension.DeployToGae
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DeployToGaeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("266caee5-128b-4fdd-a22d-d7a7c8017794");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeployToGae"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private DeployToGaeCommand(Package package)
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
                var menuItem = new OleMenuCommand(this.DeployHandler, menuCommandID);
                menuItem.BeforeQueryStatus += QueryStatusHandler;

                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DeployToGaeCommand Instance { get; private set; }

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
            Instance = new DeployToGaeCommand(package);
        }

        private void DeployHandler(object sender, EventArgs e)
        {
            var startupProject = SolutionHelper.CurrentSolution?.StartupProject;
            if (startupProject == null)
            {
                return;
            }

            // Validate the environment, possibly show an error if not valid.
            if (!CommandUtils.ValidateEnvironment(this.ServiceProvider))
            {
                return;
            }

            var window = new DeploymentDialogWindow(new DeploymentDialogWindowOptions
            {
                Project = startupProject,
                ProjectsToRestore = SolutionHelper.CurrentSolution.Projects,
            });
            window.ShowModal();
        }

        private void QueryStatusHandler(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            // Enable and show the item only if there's an existing Dnx startup project.
            var startupProject = SolutionHelper.CurrentSolution?.StartupProject;
            bool isDnxProject = false;

            if (startupProject != null)
            {
                isDnxProject = startupProject.Runtime != DnxRuntime.None && startupProject.HasWebServer;
            }

            bool isComandEnabled = !GoogleCloudExtensionPackage.IsDeploying && isDnxProject;
            menuCommand.Visible = true;
            menuCommand.Enabled = isComandEnabled;
            if (isComandEnabled)
            {
                menuCommand.Text = $"Deploy {startupProject.Name} to AppEngine...";
            }
            else
            {
                menuCommand.Text = $"Deploy Project to AppEngine...";
            }
        }
    }
}
