﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.MenuBarControls
{
    [Guid(GuidString)]
    public class GcpMenuBarControlFactory : IVsUIFactory
    {
        public const string GuidString = "36E3BEDB-7C67-404C-B1BC-28B6A87E779A";
        public const int GcpMenuBarControl = 100;

        /// <summary>Creates an instance of the specified element.</summary>
        /// <param name="guid">The GUID of the command.</param>
        /// <param name="dw">The command ID. </param>
        /// <param name="uiElement">[out] The element that was created.</param>
        /// <returns>If the method succeeds, it returns <see cref="F:Microsoft.VisualStudio.VSConstants.S_OK" />. If it fails, it returns an error code.</returns>
        public int CreateUIElement(ref Guid guid, uint dw, out IVsUIElement uiElement)
        {
            uiElement = new GcpMenuBarControl();
            return VSConstants.S_OK;
        }
    }
}