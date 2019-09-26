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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// Interaction logic for AppEngineAppsToolWindowControl.
    /// </summary>
    public partial class CloudExplorerToolWindowControl : UserControl
    {
        private readonly ISelectionUtils _selectionUtils;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudExplorerToolWindowControl"/> class.
        /// </summary>
        public CloudExplorerToolWindowControl(ISelectionUtils selectionUtils)
        {
            InitializeComponent();
            _selectionUtils = selectionUtils;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ErrorHandlerUtils.HandleExceptionsAsync(async () =>
            {
                if (e.OldValue is ICloudExplorerItemSource oldItemSource)
                {
                    oldItemSource.ItemChanged -= OnItemChanged;
                }

                if (e.NewValue is ICloudExplorerItemSource newItemSource)
                {
                    newItemSource.ItemChanged += OnItemChanged;
                    await _selectionUtils.SelectItemAsync(newItemSource.Item);
                }
                else
                {
                    await _selectionUtils.ClearSelectionAsync();
                }
            });
        }

        private void OnItemChanged(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleExceptionsAsync(
                async () =>
                {
                    var itemSource = (ICloudExplorerItemSource)sender;
                    await _selectionUtils.SelectItemAsync(itemSource.Item);
                });
        }

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                // Detect that Shift+F10 is pressed, open up the context menu.
                if (e.Key == Key.F10 && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    if (_treeView.SelectedItem is TreeHierarchy item)
                    {
                        item.ContextMenu.IsOpen = true;
                        e.Handled = true;
                    }
                }
            });
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                if (sender is TreeViewItem item)
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
                if (item?.Header is TreeNode node)
                {
                    // If the node doesn't have a context menu defined then declare the event as
                    // handled so no context menu is shown.
                    e.Handled = node.ContextMenu == null;
                    node.OnMenuItemOpen();
                }
            });
        }
    }
}
