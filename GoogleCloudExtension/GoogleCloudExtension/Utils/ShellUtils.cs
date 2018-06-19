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

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using VSOLEInterop = Microsoft.VisualStudio.OLE.Interop;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains utilities to manage the UI state of the Visual Studio shell.
    /// </summary>
    [Export(typeof(IShellUtils))]
    public class ShellUtils : IShellUtils
    {
        public static IShellUtils Default => GoogleCloudExtensionPackage.Instance.ShellUtils;

        /// <summary>
        /// Returns whether the shell is in the debugger state. This will happen if the user is debugging an app.
        /// </summary>
        /// <returns>True if the shell is in the debugger state, false otherwise.</returns>
        public bool IsDebugging()
        {
            var monitorSelection = GetMonitorSelectionService();
            return GetUIContext(monitorSelection, VSConstants.UICONTEXT.Debugging_guid);
        }

        /// <summary>
        /// Returns whether the shell is in the buliding state. This will happen if the user is building the app.
        /// </summary>
        /// <returns>True if the shell is in the building state, false otherwise.</returns>
        public bool IsBuilding()
        {
            var monitorSelection = GetMonitorSelectionService();
            return GetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionBuilding_guid);
        }

        /// <summary>
        /// Returns true if the shell is in a busy state.
        /// </summary>
        public bool IsBusy() => IsDebugging() || IsBuilding();

        /// <summary>
        /// Changes the UI state to a busy state. The pattern to use this method is to assign the result value
        /// to a variable in a using statement.
        /// </summary>
        /// <returns>An implementation of <seealso cref="IDisposable"/> that will cleanup the state change on dispose.</returns>
        public IDisposable SetShellUIBusy()
        {
            IVsMonitorSelection monitorSelection = GetMonitorSelectionService();

            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionBuilding_guid, true);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.NotBuildingAndNotDebugging_guid, false);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, false);

            return new Disposable(SetShellNormal);
        }

        /// <summary>
        /// Updates the UI state of all commands in the VS shell. This is useful when the state that determines
        /// if a command is enabled/disabled (or visible/invisiable) changes and the commands in the menus need
        /// to be updated.
        /// In essence this method will cause the <seealso cref="OleMenuCommand.BeforeQueryStatus"/> event in all commands to be
        /// triggered again.
        /// </summary>
        public void InvalidateCommandsState()
        {
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                Debug.WriteLine($"Could not acquire {nameof(SVsUIShell)}");
                return;
            }

            // Updates the UI asynchronously.
            shell.UpdateCommandUI(fImmediateUpdate: 0);
        }

        /// <summary>
        /// Attempts to move the VS window to the foreground.
        /// </summary>
        public void SetForegroundWindow()
        {
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                Debug.WriteLine($"Could not acquire {nameof(SVsUIShell)}");
                return;
            }

            if (shell.SetForegroundWindow() != VSConstants.S_OK)
            {
                Debug.WriteLine($"Failed to call SetForegroundWindow.");
            }
        }

        /// <summary>
        /// Executes the "File.OpenProject" command in the shell.
        /// </summary>
        public void OpenProject()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            dte.ExecuteCommand("File.OpenProject");
        }

        /// <summary>
        /// Opens a source file in Visual Studio.
        /// </summary>
        /// <param name="sourceFile">Source file path</param>
        /// <returns>The Window that displays the project item.</returns>
        public Window Open(string sourceFile)
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            Window window = dte.ItemOperations.OpenFile(sourceFile);
            if (window != null)
            {
                window.Visible = true;
            }
            return window;
        }

        /// <summary>
        /// Get Visual Studio <seealso cref="IServiceProvider"/>.
        /// </summary>
        public ServiceProvider GetGloblalServiceProvider()
        {
            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            VSOLEInterop.IServiceProvider sp = (VSOLEInterop.IServiceProvider)dte2;
            return new ServiceProvider(sp);
        }

        /// <summary>
        /// Executes the "File.SaveAll" command in the shell, which will save all currently dirty files.
        /// </summary>
        public void SaveAllFiles()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            dte.ExecuteCommand("File.SaveAll");
        }

        /// <summary>
        /// Register a Visual Studio Window close event handler.
        /// </summary>
        /// <param name="onWindowCloseEventHandler">The event handler.</param>
        public void RegisterWindowCloseEventHandler(Action<Window> onWindowCloseEventHandler)
        {
            var dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            dte2.Events.WindowEvents.WindowClosing += (window) => onWindowCloseEventHandler(window);
        }

        /// <summary>
        /// Create empty solution at the <paramref name="localPath"/>
        /// </summary>
        /// <param name="localPath">Create the solution at the path.</param>
        /// <param name="name">The solution name.</param>
        public void CreateEmptySolution(string localPath, string name)
        {
            localPath.ThrowIfNullOrEmpty(nameof(localPath));
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            try
            {
                dte.Solution.Create(localPath, name);
                dte.Solution.Close(false);
            }
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            { }
        }

        /// <summary>
        /// Open a create solution dialog on the given path.
        /// </summary>
        /// <param name="path">The initial path in the create solution dialog.</param>
        public void LaunchCreateSolutionDialog(string path)
        {
            path.ThrowIfNullOrEmpty(nameof(path));
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            // Set default project location
            // Refer to https://msdn.microsoft.com/en-us/library/ms165643.aspx
            var locationItem = dte.Properties["Environment", "ProjectsAndSolution"].Item("ProjectsLocation");
            if (locationItem != null)
            {
                locationItem.Value = path;
            }

            var solution = GetGloblalServiceProvider()?.GetService(typeof(SVsSolution)) as IVsSolution;
            solution?.CreateNewProjectViaDlg(null, null, 0);
        }

        private void SetShellNormal()
        {
            var monitorSelection = GetMonitorSelectionService();
            var isDebugging = IsDebugging();

            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionBuilding_guid, false);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.NotBuildingAndNotDebugging_guid, isDebugging);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, isDebugging);
        }

        private IVsMonitorSelection GetMonitorSelectionService()
        {
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection == null)
            {
                Debug.WriteLine($"Could not acquire {nameof(SVsShellMonitorSelection)}");
                throw new InvalidOperationException("Cannot set shell state.");
            }

            return monitorSelection;
        }

        private void SetUIContext(IVsMonitorSelection monitorSelection, Guid contextGuid, bool value)
        {
            uint cookie = 0;

            ErrorHandler.ThrowOnFailure(monitorSelection.GetCmdUIContextCookie(contextGuid, out cookie));
            if (cookie != 0)
            {
                ErrorHandler.ThrowOnFailure(monitorSelection.SetCmdUIContext(cookie, value ? 1 : 0));
            }
        }

        private bool GetUIContext(IVsMonitorSelection monitorSelection, Guid contextGuid)
        {
            uint cookie = 0;
            int isActive = 0;

            ErrorHandler.ThrowOnFailure(monitorSelection.GetCmdUIContextCookie(contextGuid, out cookie));
            if (cookie != 0)
            {
                ErrorHandler.ThrowOnFailure(monitorSelection.IsCmdUIContextActive(cookie, out isActive));
            }

            return isActive != 0;
        }
    }
}
