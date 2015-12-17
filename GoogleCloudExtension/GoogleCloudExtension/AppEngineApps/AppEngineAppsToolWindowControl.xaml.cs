// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using System;
using System.Windows.Controls;

namespace GoogleCloudExtension.AppEngineApps
{
    /// <summary>
    /// Interaction logic for AppEngineAppsToolWindowControl.
    /// </summary>
    public partial class AppEngineAppsToolWindowControl : UserControl
    {
        /// <summary>
        /// Event raised whenever the currently selected object changes.
        /// </summary>
        public event EventHandler<ModuleAndVersion> SelectedItemChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppEngineAppsToolWindowControl"/> class.
        /// </summary>
        public AppEngineAppsToolWindowControl()
        {
            this.InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            var newValue = e.NewValue as ModuleAndVersion;
            if (newValue != null)
            {
                SelectedItemChanged?.Invoke(this, newValue);
            }
        }
    }
}
