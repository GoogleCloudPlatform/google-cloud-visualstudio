// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    internal static class SelectionUtils
    {
        internal static bool ActivatePropertiesWindow(IServiceProvider provider)
        {
            IVsWindowFrame frame = null;
            var shell = provider.GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                Debug.WriteLine("Could not get the shell.");
                return false;
            }

            var guidPropertyBrowser = new Guid(ToolWindowGuids.PropertyBrowser);
            shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guidPropertyBrowser, out frame);
            if (frame == null)
            {
                Debug.WriteLine("Failed to get the frame.");
                return false;
            }

            frame.Show();
            return true;
        }

        internal static void ClearSelection(IServiceProvider provider)
        {
            var selectionTracker = provider.GetService(typeof(STrackSelection)) as ITrackSelection;
            if (selectionTracker == null)
            {
                Debug.WriteLine("Failed to find the selection tracker.");
                return;
            }

            var selectionContainer = new SelectionContainer();
            selectionContainer.SelectedObjects = new List<object>();
            selectionTracker.OnSelectChange(selectionContainer);
        }

        internal static void SelectItem(IServiceProvider provider, object item)
        {
            var selectionTracker = provider.GetService(typeof(STrackSelection)) as ITrackSelection;
            if (selectionTracker == null)
            {
                Debug.WriteLine("Failed to find the selection tracker.");
                return;
            }

            var selectionContainer = new SelectionContainer();
            selectionContainer.SelectedObjects = new List<object> { item };

            Debug.WriteLine($"Updated selected object: {item}");
            selectionTracker.OnSelectChange(selectionContainer);
        }
    }
}
