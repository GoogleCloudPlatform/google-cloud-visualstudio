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
using System.Windows.Input;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// Interaction logic for AppEngineAppsToolWindowControl.
    /// </summary>
    public partial class CloudExplorerToolWindowControl : UserControl
    {
        private readonly SelectionUtils _selectionUtils;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudExplorerToolWindowControl"/> class.
        /// </summary>
        public CloudExplorerToolWindowControl(SelectionUtils selectionUtils)
        {
            this.InitializeComponent();
            _selectionUtils = selectionUtils;
        }

        private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                var oldItemSource = e.OldValue as ICloudExplorerItemSource;
                if (oldItemSource != null)
                {
                    oldItemSource.ItemChanged -= OnItemChanged;
                }

                var newItemSource = e.NewValue as ICloudExplorerItemSource;
                if (newItemSource == null)
                {
                    _selectionUtils.ClearSelection();
                    return;
                }
                newItemSource.ItemChanged += OnItemChanged;

                _selectionUtils.SelectItem(newItemSource.Item);
            });
        }

        private void OnItemChanged(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                var itemSource = (ICloudExplorerItemSource)sender;
                _selectionUtils.SelectItem(itemSource.Item);
            });
        }

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                // Detect that Shift+F10 is pressed, open up the context menu.
                if (e.Key == Key.F10 && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    var item = _treeView.SelectedItem as TreeHierarchy;
                    if (item != null)
                    {
                        var contextMenu = item.ContextMenu;
                        contextMenu.IsOpen = true;
                        e.Handled = true;
                    }
                }
            });
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                var item = sender as TreeViewItem;
                if (item != null)
                {
                    item.IsSelected = true;
                }
            });
        }

        private void TreeViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                var item = sender as TreeViewItem;
                if (item != null)
                {
                    var node = item.Header as TreeNode;
                    if (node != null)
                    {
                        // If the node doesn't have a context menu defined then declare the event as
                        // handled so no context menu is shown.
                        e.Handled = node.ContextMenu == null;
                    }
                }
            });
        }
    }
}
