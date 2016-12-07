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

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GoogleCloudExtension.Utils
{
    public static class DataGridBehaviors
    {
        #region HasCustomSort property.

        public static readonly DependencyProperty HasCustomSortProperty =
            DependencyProperty.RegisterAttached(
                "HasCustomSort",
                typeof(bool),
                typeof(DataGridBehaviors),
                new PropertyMetadata(false, OnHasCustomSortPropertyChanged));

        public static bool GetHasCustomSort(DataGrid self) => (bool)self.GetValue(HasCustomSortProperty);

        public static void SetHasCustomSort(DataGrid self, bool value)
        {
            self.SetValue(HasCustomSortProperty, value);
        }

        private static void OnHasCustomSortPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as DataGrid;
            if (self == null)
            {
                Debug.WriteLine($"Attempted to use {nameof(HasCustomSortProperty)} on type {d.GetType().Name}");
                return;
            }

            var oldValue = (bool)e.OldValue;
            var newValue = (bool)e.NewValue;

            if (oldValue)
            {
                self.Sorting -= OnDataGridSorting;
            }

            if (newValue)
            {
                self.Sorting += OnDataGridSorting;
            }
        }

        #endregion

        #region CustomSort property.

        public static readonly DependencyProperty CustomSortProperty =
            DependencyProperty.RegisterAttached(
                "CustomSort",
                typeof(IColumnSorter),
                typeof(DataGridBehaviors));

        public static IColumnSorter GetCustomSort(DataGridColumn self) => (IColumnSorter)self.GetValue(CustomSortProperty);

        public static void SetCustomSort(DataGridColumn self, IColumnSorter value)
        {
            self.SetValue(CustomSortProperty, value);
        }

        #endregion

        #region Double click command.

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached(
                "DoubleClickCommand",
                typeof(ICommand),
                typeof(DataGridBehaviors),
                new PropertyMetadata(OnDoubleClickCommandPropertyChanged));

        public static ICommand GetDoubleClickCommand(DataGrid self) => (ICommand)self.GetValue(DoubleClickCommandProperty);

        public static void SetDoubleClickCommand(DataGrid self, ICommand value)
        {
            self.SetValue(DoubleClickCommandProperty, value);
        }

        private static void OnDoubleClickCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DataGrid)d;

            if (e.OldValue != null && e.NewValue == null)
            {
                self.MouseDoubleClick -= OnDataGridDoubleClick;
            }

            if (e.NewValue != null && e.OldValue == null)
            {
                self.MouseDoubleClick += OnDataGridDoubleClick;
            }
        }

        private static void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var self = (DataGrid)sender;

            var selected = self.SelectedItem;
            if (selected == null)
            {
                return;
            }

            ICommand command = GetDoubleClickCommand(self);
            if (!command.CanExecute(selected))
            {
                return;
            }

            command.Execute(selected);
        }

        #endregion

        private static void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
        {
            var self = (DataGrid)sender;
            var column = e.Column;

            var customSorter = GetCustomSort(column);
            if (customSorter == null)
            {
                // No custom sort is defined for this column, revert to the built-in sorting.
                return;
            }

            var oldIsDescending = column.SortDirection == System.ComponentModel.ListSortDirection.Descending;
            var newIsDescending = !oldIsDescending;

            var collectionView = self.ItemsSource as ListCollectionView;
            if (collectionView == null)
            {
                Debug.WriteLine($"Was unable to find collection view, found {self.ItemsSource?.GetType().Name}");
                return;
            }

            column.SortDirection = newIsDescending ? System.ComponentModel.ListSortDirection.Descending : System.ComponentModel.ListSortDirection.Ascending;
            collectionView.CustomSort = new DataGridColumnCustomSorter(customSorter, newIsDescending);

            e.Handled = true;
        }
    }
}
