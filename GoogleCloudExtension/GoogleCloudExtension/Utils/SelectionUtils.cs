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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class provides helpers to update the properties window, manage the currently selected "item".
    /// </summary>
    internal static class SelectionUtils
    {
        /// <summary>
        /// Activates the properties window, ensuring it is visible to the user.
        /// </summary>
        /// <param name="provider">The <seealso cref="IServiceProvider"/> to use to get services.</param>
        /// <returns>True if the window as activated, false otherwise.</returns>
        public static bool ActivatePropertiesWindow(IServiceProvider provider)
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

        /// <summary>
        /// Clears the selection, making the property window empty.
        /// </summary>
        /// <param name="provider">The <seealso cref="IServiceProvider"/> to use to get services.</param>
        public static void ClearSelection(IServiceProvider provider)
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

        /// <summary>
        /// Selects the given item, showing it in the properties window.
        /// </summary>
        /// <param name="provider">The <seealso cref="IServiceProvider"/> to use to get services.</param>
        /// <param name="item">The item to be selected.</param>
        public static void SelectItem(IServiceProvider provider, object item)
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
