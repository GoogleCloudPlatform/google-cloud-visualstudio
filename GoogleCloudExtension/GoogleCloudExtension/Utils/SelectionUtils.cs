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
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class provides helpers to update the properties window, manage the currently selected "item".
    /// </summary>
    public class SelectionUtils : ISelectionUtils
    {
        private readonly ToolWindowPane _owner;
        private readonly Lazy<ITrackSelection> _selectionTracker;
        private bool _propertiesWindowTickled = false;

        private IVsWindowFrame OwnerFrame
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return (IVsWindowFrame)_owner.Frame;
            }
        }

        public SelectionUtils(ToolWindowPane owner)
        {
            _owner = owner;
            _selectionTracker = new Lazy<ITrackSelection>(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return CreateTrackSelection(_owner);
            });
        }

        public void ActivatePropertiesWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var frame = GetPropertiesWindow(_owner, forceCreate: true);
            frame?.Show();
        }

        /// <summary>
        /// Clears the selection, making the property window empty.
        /// </summary>
        public async Task ClearSelectionAsync()
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            var selectionContainer = new SelectionContainer();
            selectionContainer.SelectedObjects = new List<object>();
            _selectionTracker.Value?.OnSelectChange(selectionContainer);

            await TicklePropertiesWindowAsync();
        }

        /// <summary>
        /// Selects the given item, showing it in the properties window.
        /// </summary>
        /// <param name="item">The item to be selected.</param>
        public async Task SelectItemAsync(object item)
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            var selectionContainer = new SelectionContainer(true, false);
            var list = new List<object> { item };
            selectionContainer.SelectedObjects = list;
            selectionContainer.SelectableObjects = list;

            Debug.WriteLine($"Update/d selected object: {item}");
            _selectionTracker.Value?.OnSelectChange(selectionContainer);

            await TicklePropertiesWindowAsync();
        }

        private static ITrackSelection CreateTrackSelection(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selectionTracker = serviceProvider.GetService(typeof(STrackSelection)) as ITrackSelection;
            if (selectionTracker == null)
            {
                Debug.WriteLine("Failed to find the selection tracker.");
            }
            return selectionTracker;
        }

        /// <summary>
        /// Returns the properties window if it already exists.
        /// </summary>
        /// <param name="serviceProvider">The service provide to use.</param>
        /// <param name="forceCreate">If true the properties window will be created if not present already.</param>
        /// <returns>The poroperties window object if it exists, null if it is yet to be created.</returns>
        private static IVsWindowFrame GetPropertiesWindow(IServiceProvider serviceProvider, bool forceCreate = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var shell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                Debug.WriteLine("Could not get the shell.");
                return null;
            }

            uint flags = forceCreate ? (uint)__VSFINDTOOLWIN.FTW_fForceCreate : (uint)__VSFINDTOOLWIN.FTW_fFindFirst;
            var guidPropertyBrowser = new Guid(ToolWindowGuids.PropertyBrowser);
            IVsWindowFrame frame;
            shell.FindToolWindow(flags, ref guidPropertyBrowser, out frame);
            if (forceCreate && frame == null)
            {
                Debug.WriteLine("Failed to create properties window.");
            }

            return frame;
        }

        /// <summary>
        /// This method implements a workaround for a bug in VS 2015 in which the properties window
        /// will not update the very first time is opened until it gains and loses focus. This method
        /// forces the focus to change to the properties window and then back to the owner frame to force
        /// this behavior.
        /// </summary>
        private async Task TicklePropertiesWindowAsync()
        {
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (_propertiesWindowTickled)
            {
                return;
            }

            // Tickle the properties window, change the focus to force the 
            // properties window to update. This apparently only needs to be done once.
            await Task.Delay(1);
            var frame = GetPropertiesWindow(_owner);
            if (frame == null || frame.IsVisible() != Microsoft.VisualStudio.VSConstants.S_OK)
            {
                // If there's no properties frame, or it is not visible, then there's nothing todo.
                return;
            }

            frame?.Show();
            await Task.Delay(1);
            OwnerFrame?.Show();
            _propertiesWindowTickled = true;
        }
    }
}
