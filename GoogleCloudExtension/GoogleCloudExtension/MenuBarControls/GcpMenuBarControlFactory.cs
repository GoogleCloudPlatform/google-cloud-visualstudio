// Copyright 2018 Google Inc. All Rights Reserved.
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

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.MenuBarControls
{
    [Guid(GuidString)]
    [Export(typeof(GcpMenuBarControlFactory))]
    public class GcpMenuBarControlFactory : IVsUIFactory
    {
        public const string GuidString = "36E3BEDB-7C67-404C-B1BC-28B6A87E779A";
        public const int GcpMenuBarControlCommandId = 100;

        private readonly Lazy<IGcpMenuBarControl> _wpfControl;

        private IGcpMenuBarControl WpfControl => _wpfControl.Value;

        [ImportingConstructor]
        public GcpMenuBarControlFactory(Lazy<IGcpMenuBarControl> control)
        {
            _wpfControl = control;
        }

        /// <summary>Creates an instance of the specified element.</summary>
        /// <param name="guid">The GUID of the command.</param>
        /// <param name="commandId">The command ID. </param>
        /// <param name="uiElement">[out] The element that was created.</param>
        /// <returns>If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK" />. If it fails, it returns an error code.</returns>
        public int CreateUIElement(ref Guid guid, uint commandId, out IVsUIElement uiElement)
        {
            if (guid != typeof(GcpMenuBarControlFactory).GUID)
            {
                uiElement = null;
                return Marshal.GetHRForException(
                    new ArgumentException($"Expected {typeof(GcpMenuBarControlFactory).GUID} but got {guid}", nameof(guid)));
            }

            if (commandId != GcpMenuBarControlCommandId)
            {
                uiElement = null;
                return Marshal.GetHRForException(
                    new ArgumentException($"Expected {GcpMenuBarControlCommandId} but got {commandId}", nameof(commandId)));
            }

            uiElement = WpfControl;
            return VSConstants.S_OK;
        }
    }
}