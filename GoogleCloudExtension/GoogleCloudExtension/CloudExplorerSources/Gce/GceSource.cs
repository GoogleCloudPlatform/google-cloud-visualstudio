﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceSource : CloudExplorerSourceBase<GceSourceRootViewModel>
    {
        private const string WindowsOnlyButtonIconPath = "CloudExplorerSources/Gce/Resources/filter.png";

        private static readonly Lazy<ImageSource> s_windowsOnlyButtonIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(WindowsOnlyButtonIconPath));

        private readonly ButtonDefinition _windowsOnlyButton;

        public GceSource(ICloudSourceContext context) : base(context)
        {
            _windowsOnlyButton = new ButtonDefinition
            {
                ToolTip = Resources.CloudExplorerGceOnlyWindowsButtonToolTip,
                Command = new WeakCommand(OnOnlyWindowsClicked),
                Icon = s_windowsOnlyButtonIcon.Value,
            };
            ActualButtons.Add(_windowsOnlyButton);
            ActualRoot.ShowOnlyWindowsInstancesChanged += OnShowOnlyWindowsInstancesChanged;
        }

        private void OnShowOnlyWindowsInstancesChanged(object sender, EventArgs e)
        {
            _windowsOnlyButton.IsChecked = ActualRoot.ShowOnlyWindowsInstances;
        }

        private void OnOnlyWindowsClicked()
        {
            _windowsOnlyButton.IsChecked = !_windowsOnlyButton.IsChecked;
            ActualRoot.ShowOnlyWindowsInstances = _windowsOnlyButton.IsChecked;
        }
    }
}
