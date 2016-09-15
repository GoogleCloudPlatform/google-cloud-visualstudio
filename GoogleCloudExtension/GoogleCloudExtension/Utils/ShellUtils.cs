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