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

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class implements WPF behaviors that make working with MVVM and <seealso cref="DataGrid"/> much easier. These
    /// behaviors are implemented as a set of attached properties that will add the necessary event handlers
    /// to the <seealso cref="DataGrid"/> without having to write code behind.
    /// </summary>
    public static class DataGridBehaviors
    {
        #region HasCustomSort property.

        /// <summary>
        /// This property allows the user to define a <seealso cref="IColumnSorter"/> to be used to
        /// sort the columns. Setting this property to <code>true</code> enables the feature. Otherwise
        /// the property <seealso cref="CustomSortProperty"/> will not do anything.
        /// </summary>
        public static readonly DependencyProperty HasCustomSortProperty =
            DependencyProperty.RegisterAttached(
                "HasCustomSort",
                typeof(bool),
                typeof(DataGridBehaviors),
                new PropertyMetadata(false, OnHasCustomSortPropertyChanged));

        /// <summary>
        /// Getter for the attached property.
        /// </summary>
        public static bool GetHasCustomSort(DataGrid self) => (bool)self.GetValue(HasCustomSortProperty);

        /// <summary>
        /// Setter for the attached property.
        /// </summary>
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

        /// <summary>
        /// This attached property  meant to be applied to <seealso cref="DataGridColumn"/> instances, and contains
        /// the <see cref="IColumnSorter"/> instance to use to sort this column.
        /// </summary>
        public static readonly DependencyProperty CustomSortProperty =
            DependencyProperty.RegisterAttached(
                "CustomSort",
                typeof(IColumnSorter),
                typeof(DataGridBehaviors));

        /// <summary>
        /// The getter for the attached property.
        /// </summary>
        public static IColumnSorter GetCustomSort(DataGridColumn self) => (IColumnSorter)self.GetValue(CustomSortProperty);

        /// <summary>
        /// The setter for the attached property.
        /// </summary>
        public static void SetCustomSort(DataGridColumn self, IColumnSorter value)
        {
            self.SetValue(CustomSortProperty, value);
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
