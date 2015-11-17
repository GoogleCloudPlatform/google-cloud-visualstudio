﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DeploymentDialog;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.DeployToGaeContextMenu
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DeployToGaeContextMenuCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4179;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("266caee5-128b-4fdd-a22d-d7a7c8017794");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeployToGaeContextMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private DeployToGaeContextMenuCommand(Package package)
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
                var menuItem = new OleMenuCommand(this.DeployToGaeHandler, menuCommandID);
                menuItem.BeforeQueryStatus += QueryStatusHandler;

                commandService.AddCommand(menuItem);
            }
        }

        private static string GetSelectedProjectPath()
        {
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return null;
            }

            IVsMultiItemSelect select = null;
            uint itemid = 0;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out select, out selectionContainerPtr);
                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    return null;
                }

                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null)
                {
                    return null;
                }

                object result = null;
                hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out result);
                return (string)result;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DeployToGaeContextMenuCommand Instance { get; private set; }

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
            Instance = new DeployToGaeContextMenuCommand(package);
        }

        private void DeployToGaeHandler(object sender, EventArgs e)
        {
            var startupProjectPath = GetSelectedProjectPath();

            // Validate the environment, possibly show an error if not valid.
            if (!CommandUtils.ValidateEnvironment(this.ServiceProvider))
            {
                return;
            }

            var window = new DeploymentDialogWindow(new DeploymentDialogWindowOptions
            {
                Project = new Project(startupProjectPath),
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

            var selectedProjectPath = GetSelectedProjectPath();
            var isDnxProject = String.IsNullOrEmpty(selectedProjectPath) ? false : Project.IsDnxProject(selectedProjectPath);
            Project project = null;

            if (isDnxProject)
            {
                project = new Project(selectedProjectPath);
                isDnxProject = project.Runtime != DnxRuntime.None && project.HasWebServer;
            }

            bool isCommandEnabled = !GoogleCloudExtensionPackage.IsDeploying && isDnxProject;
            menuCommand.Visible = isDnxProject;
            menuCommand.Enabled = isCommandEnabled;
            if (isCommandEnabled)
            {
                menuCommand.Text = $"Deploy {project.Name} to AppEngine...";
            }
        }
    }
}
