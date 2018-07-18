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

using Microsoft.VisualStudio.Shell;
using System;

namespace GoogleCloudExtension.MenuBarControls
{
    public class ProvideMainWindowFrameControlAttribute : RegistrationAttribute
    {
        private readonly Type _controlType;
        private readonly int _viewId;
        private readonly Type _factoryType;

        public ProvideMainWindowFrameControlAttribute(Type controlType, int viewId, Type factoryType)
        {
            _controlType = controlType;
            _viewId = viewId;
            _factoryType = factoryType;
        }

        public override void Register(RegistrationContext context)
        {
            using (Key key = context.CreateKey(MainFrameControlKey))
            {
                key.SetValue(null, "GCP Project Card");
                key.SetValue("DisplayName", "#1000");
                key.SetValue("Alignment", "MenuBarRight");
                key.SetValue("FullScreenAlignment", "MenuBarRight");
                key.SetValue("Sort", 700);
                key.SetValue("FullScreenSort", 700);
                key.SetValue("ViewFactory", _factoryType.GUID);
                key.SetValue("ViewId", _viewId);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(MainFrameControlKey);
        }

        private string MainFrameControlKey => @"MainWindowFrameControls\" + _controlType.GUID.ToString("B");
    }
}
