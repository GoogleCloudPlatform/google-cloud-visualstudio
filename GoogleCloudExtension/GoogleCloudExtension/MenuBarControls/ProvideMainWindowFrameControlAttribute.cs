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
using Microsoft.VisualStudio.Shell;

namespace GoogleCloudExtension.MenuBarControls
{
    /// <summary>
    /// This attribute registers a control to be placed on the Visual Studio Main Window Frame.
    /// </summary>
    public class ProvideMainWindowFrameControlAttribute : RegistrationAttribute
    {
        public enum AlignmentEnum
        {
            MenuBarRight
        }

        private const string BraceGuidFormat = "B";
        private readonly Type _controlType;
        private readonly int _viewId;
        private readonly Type _factoryType;

        /// <summary>
        /// The human readable default value of the new registry key.
        /// </summary>
        public string Name { get; set; } = "GCP Project Card";

        /// <summary>
        /// The key of the VSPackage.resx resource that will be the display name of the control. Defaults to #1000.
        /// </summary>
        public string DisplayNameResourceKey { get; set; } = "#1000";

        /// <summary>
        /// The sort value. Defaults to 550, which is 50 less (to the left of) the existing Microsoft user card.
        /// </summary>
        public int Sort { get; set; } = 550;

        /// <summary>
        /// The Alignment of the menu bar item. Defaults to MenuBarRight.
        /// </summary>
        public AlignmentEnum Alignment { get; set; } = AlignmentEnum.MenuBarRight;

        /// <summary>
        /// Creates an attribute to register a control to be placed on the Visual Studio Main Window Frame.
        /// </summary>
        /// <param name="controlType">
        /// The type of control to register. This type should implement
        /// <see cref="Microsoft.VisualStudio.Shell.Interop.IVsUIElement"/>.
        /// </param>
        /// <param name="viewId">
        /// The id of the control. The factory will be given this id when asked to create the control.
        /// </param>
        /// <param name="factoryType">
        /// The type of <see cref="Microsoft.VisualStudio.Shell.Interop.IVsUIFactory"/>
        /// that will be used to create the control. This factory must be registered by both a
        /// <see cref="Microsoft.Internal.VisualStudio.PlatformUI.ProvideUIProviderAttribute"/> and
        /// <see cref="Microsoft.VisualStudio.Shell.Interop.IVsRegisterUIFactories.RegisterUIFactory"/>.
        /// </param>
        public ProvideMainWindowFrameControlAttribute(Type controlType, int viewId, Type factoryType)
        {
            _controlType = controlType;
            _viewId = viewId;
            _factoryType = factoryType;
        }

        /// <summary>
        /// Registers a new subkey of MainFrameControls for the given control.
        /// </summary>
        public override void Register(RegistrationContext context)
        {
            using (Key key = context.CreateKey(MainFrameControlKey))
            {
                key.SetValue(null, Name);
                key.SetValue("DisplayName", DisplayNameResourceKey);

                key.SetValue("Package", context.ComponentType.GUID.ToString(BraceGuidFormat));
                key.SetValue("ViewFactory", _factoryType.GUID.ToString(BraceGuidFormat));
                key.SetValue("ViewId", _viewId);

                key.SetValue("Alignment", Alignment.ToString());
                key.SetValue("FullScreenAlignment", Alignment.ToString());
                key.SetValue("Sort", Sort);
                key.SetValue("FullScreenSort", Sort);
            }
        }

        /// <summary>
        /// Removes the control specific subkey of MainFrameControls.
        /// </summary>
        public override void Unregister(RegistrationContext context) => context.RemoveKey(MainFrameControlKey);

        private string MainFrameControlKey => @"MainWindowFrameControls\" + _controlType.GUID.ToString(BraceGuidFormat);
    }
}
