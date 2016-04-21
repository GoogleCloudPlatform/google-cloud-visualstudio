// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

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
        private bool _propertiesWindowActivated = false;
        private readonly WeakAction<object, EventArgs> _onItemChangedHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudExplorerToolWindowControl"/> class.
        /// </summary>
        public CloudExplorerToolWindowControl(IServiceProvider provider)
        {
            this.InitializeComponent();
            _provider = provider;
            _onItemChangedHandler = new WeakAction<object, EventArgs>(OnItemChanged);
        }

        private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            var oldItemSource = e.OldValue as ICloudExplorerItemSource;
            if (oldItemSource != null)
            {
                oldItemSource.ItemChanged -= OnItemChanged;
            }

            var itemSource = e.NewValue as ICloudExplorerItemSource;
            if (itemSource == null)
            {
                SelectionUtils.ClearSelection(_provider);
                return;
            }
            itemSource.ItemChanged += OnItemChanged;

            var item = itemSource.Item;
            if (!_propertiesWindowActivated)
            {
                _propertiesWindowActivated = SelectionUtils.ActivatePropertiesWindow(_provider);
            }
            SelectionUtils.SelectItem(_provider, item);
        }

        private void OnItemChanged(object sender, EventArgs e)
        {
            var itemSource = (ICloudExplorerItemSource)sender;
            SelectionUtils.SelectItem(_provider, itemSource.Item);
        }
    }
}
