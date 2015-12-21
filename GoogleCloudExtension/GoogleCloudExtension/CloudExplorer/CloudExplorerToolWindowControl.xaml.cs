// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// Interaction logic for AppEngineAppsToolWindowControl.
    /// </summary>
    public partial class CloudExplorerToolWindowControl : UserControl
    {
        private readonly IServiceProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudExplorerToolWindowControl"/> class.
        /// </summary>
        public CloudExplorerToolWindowControl(IServiceProvider provider)
        {
            this.InitializeComponent();
            _provider = provider;
        }

        private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            var itemSource = e.NewValue as ICloudExplorerItem;
            if (itemSource == null)
            {
                return;
            }

            var item = itemSource.Item;
            SelectionUtils.SelectItem(_provider, item);
        }
    }
}
