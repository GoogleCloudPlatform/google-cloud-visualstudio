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

            var newItemSource = e.NewValue as ICloudExplorerItemSource;
            if (newItemSource == null)
            {
                SelectionUtils.ClearSelection(_provider);
                return;
            }
            newItemSource.ItemChanged += OnItemChanged;

            var item = newItemSource.Item;
            if (!_propertiesWindowActivated)
            {
                // The properties window can only be activated once, to avoid it stealing focus continously.
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
