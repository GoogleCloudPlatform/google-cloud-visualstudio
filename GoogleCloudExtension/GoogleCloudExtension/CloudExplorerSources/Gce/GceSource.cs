// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceSource : CloudExplorerSourceBase<GceSourceRootViewModel>
    {
        private const string WindowsOnlyButtonIconPath = "CloudExplorerSources/Gce/Resources/gce_logo.png";

        private static readonly Lazy<ImageSource> s_windowsOnlyButtonIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(WindowsOnlyButtonIconPath));

        private readonly ButtonDefinition _windowsOnlyButton;

        public GceSource()
        {
            _windowsOnlyButton = new ButtonDefinition
            {
                ToolTip = "Only Windows Instances",
                Command = new WeakCommand(OnOnlyWindowsClicked),
                Icon = s_windowsOnlyButtonIcon.Value,
            };
            ActualButtons.Add(_windowsOnlyButton);
        }

        private void OnOnlyWindowsClicked()
        {
            _windowsOnlyButton.IsChecked = !_windowsOnlyButton.IsChecked;
            ActualRoot.ShowOnlyWindowsInstances = _windowsOnlyButton.IsChecked;
        }
    }
}
