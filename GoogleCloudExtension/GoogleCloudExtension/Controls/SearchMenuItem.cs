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

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Custom menu item that use a search box to filter the list of items.
    /// </summary>
    [TemplatePart(Name = "PART_searchTextBox", Type = typeof(TextBox))]
    public class SearchMenuItem : MenuItem
    {
        private TextBox _searchBox;

        public static DependencyProperty OnSubmenuOpenCommandProperty = DependencyProperty.Register(
            "OnSubmenuOpenCommand", typeof(ICommand), typeof(SearchMenuItem));

        public static DependencyProperty IsSubmenuPopulatedProperty = DependencyProperty.Register(
            "IsSubmenuPopulated", typeof(bool), typeof(SearchMenuItem), new FrameworkPropertyMetadata(true));

        public static DependencyProperty ChooseAllHeaderProperty = DependencyProperty.Register(
            "ChooseAllHeader", typeof(string), typeof(SearchMenuItem), 
            new FrameworkPropertyMetadata(GoogleCloudExtension.Resources.UiChooseAllMenuHeader));

        /// <summary>
        /// Gets or sets the choose all menu item header depenedency propoerty.
        /// </summary>
        public string ChooseAllHeader
        {
            get { return (string)GetValue(ChooseAllHeaderProperty); }
            set { SetValue(ChooseAllHeaderProperty, value); }
        }

        /// <summary>
        /// Get or set the dependency property <seealso cref="OnSubmenuOpenCommandProperty"/>.
        /// The command is called when submenu popup opens.
        /// </summary>
        public ICommand OnSubmenuOpenCommand
        {
            get { return (ICommand)GetValue(OnSubmenuOpenCommandProperty); }
            set { SetValue(OnSubmenuOpenCommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets the dependency propoerty <seealso cref="IsSubmenuPopulatedProperty"/>
        /// It indicates if the submenu items are populated.
        /// This property supports delay load submenu items.
        /// </summary>
        public bool IsSubmenuPopulated
        {
            get { return (bool)GetValue(IsSubmenuPopulatedProperty); }
            set { SetValue(IsSubmenuPopulatedProperty, value); }
        }

        /// <summary>
        /// Create bindings and event handlers on named items.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _searchBox = Template.FindName("PART_searchTextBox", this) as TextBox;
            if (_searchBox != null)
            {
                _searchBox.TextChanged += OnSearchBoxTextChanged;
            }
            SubmenuOpened += OnSubmenuOpened;
        }

        /// <summary>
        /// By default it returns MenuItem. Override to return SearchMenuItem.
        /// The is the key part to make the HierarchicalDataTemplate data binding work.
        /// </summary>
        /// <returns>A <seealso cref="SearchMenuItem"/> object.</returns>
        protected override DependencyObject GetContainerForItemOverride() => new SearchMenuItem();

        private void OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"{Header} submenu opened");
            if (OnSubmenuOpenCommand != null && OnSubmenuOpenCommand.CanExecute(null) && !IsSubmenuPopulated)
            {
                OnSubmenuOpenCommand.Execute(null);
            }
        }

        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var prefix = _searchBox.Text.Trim();
            for (int i = 0; i < Items.Count; i++)
            {
                MenuItem menuItem = ItemContainerGenerator.ContainerFromIndex(i) as MenuItem;
                string label = menuItem?.Header?.ToString();
                if (label == null)
                {
                    continue;
                }

                menuItem.Visibility = prefix == "" ||
                    label.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase) ?
                    Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
