using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    public static class ShellUtils
    {
        public static void InvalidateCommandUIStatus()
        {
            // Invalidate the commands status.                                                                                                                                                                                                
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                return;
            }
            shell.UpdateCommandUI(0);
        }

        public static bool IsDebugging()
        {
            var monitorSelection = GetMonitorSelectionService();
            return GetUIContext(monitorSelection, VSConstants.UICONTEXT.Debugging_guid);
        }

        public static bool IsBuilding()
        {
            var monitorSelection = GetMonitorSelectionService();
            return GetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionBuilding_guid);
        }

        public static bool IsBusy() => IsDebugging() || IsBuilding();

        public static IDisposable SetShellUIBusy()
        {
            IVsMonitorSelection monitorSelection = GetMonitorSelectionService();

            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionBuilding_guid, true);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.NotBuildingAndNotDebugging_guid, false);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, false);

            return new Disposable(SetShellNormal);
        }

        private static void SetShellNormal()
        {
            var monitorSelection = GetMonitorSelectionService();
            var isDebugging = IsDebugging();

            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionBuilding_guid, false);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.NotBuildingAndNotDebugging_guid, isDebugging);
            SetUIContext(monitorSelection, VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, isDebugging);
        }

        private static IVsMonitorSelection GetMonitorSelectionService()
        {
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection == null)
            {
                Debug.WriteLine($"Could not acquire {nameof(SVsShellMonitorSelection)}");
                throw new InvalidOperationException("Cannot set shell state.");
            }

            return monitorSelection;
        }

        private static void SetUIContext(IVsMonitorSelection monitorSelection, Guid contextGuid, bool value)
        {

            uint cookie = 0;

            ErrorHandler.ThrowOnFailure(monitorSelection.GetCmdUIContextCookie(contextGuid, out cookie));
            if (cookie != 0)
            {
                ErrorHandler.ThrowOnFailure(monitorSelection.SetCmdUIContext(cookie, value ? 1 : 0));
            }
        }

        private static bool GetUIContext(IVsMonitorSelection monitorSelection, Guid contextGuid)
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