// Copyright 2017 Google Inc. All Rights Reserved.
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorer.Options
{
    /// <summary>
    /// Interaction logic for CloudExplorerOptionsPage.xaml
    /// </summary>
    public partial class CloudExplorerOptionsPage
    {
        /// <summary>
        /// The <see cref="CloudExplorerOptionsPageViewModel"/> that is the <see cref="FrameworkElement.DataContext"/>.
        /// </summary>
        public CloudExplorerOptionsPageViewModel ViewModel { get; }

        /// <summary>
        /// Creates and initializes a new <see cref="CloudExplorerOptionsPage"/>.
        /// </summary>
        /// <param name="parentModel"></param>
        public CloudExplorerOptionsPage(ICloudExplorerOptions parentModel)
        {
            ViewModel = new CloudExplorerOptionsPageViewModel(parentModel.ResetSettings);
            DataContext = ViewModel;
            parentModel.SavingSettings += UpdateBindingSources;
            AddHandler(UIElementDialogPage.DialogKeyPendingEvent, new RoutedEventHandler(OnDialogKeyPending));
            InitializeComponent();
        }

        /// <summary>
        /// Updates the sources of the data input bindings.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="args">Unused.</param>
        private void UpdateBindingSources(object sender, EventArgs args)
        {
            _pubSubFilters.CommitEdit();
            _pubSubFilters.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateSource();
        }

        /// <summary>
        /// Handles the <see cref="UIElementDialogPage.DialogKeyPendingEvent"/>,
        /// allowing this WPF element to catch enter and escape keys during editing mode.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="args">
        /// Updates the <see cref="RoutedEventArgs.Handled"/> property.
        /// </param>
        private void OnDialogKeyPending(object sender, RoutedEventArgs args)
        {
            IEditableCollectionView itemCollection = _pubSubFilters.Items;
            args.Handled = itemCollection.IsEditingItem || itemCollection.IsAddingNew;
        }
    }
}
