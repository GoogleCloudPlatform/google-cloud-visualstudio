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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Interaction logic for LogsViewerToolWindowControl.
    /// </summary>
    public partial class LogsViewerToolWindowControl : UserControl
    {
        private LogsViewerViewModel ViewModel => DataContext as LogsViewerViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsViewerToolWindowControl"/> class.
        /// </summary>
        public LogsViewerToolWindowControl()
        {
            this.InitializeComponent();
        }
 
        /// <summary>
        /// Get the first ancestor control element of type TControl.
        /// </summary>
        /// <typeparam name="TControl">A <seealso cref="Control"/> type.</typeparam>
        /// <param name="dependencyObj">A <seealso cref="DependencyObject"/> element. </param>
        /// <returns>null or TControl object.</returns>
        private TControl FindAncestorControl<TControl>(DependencyObject dependencyObj) where TControl : Control
        {
            while ((dependencyObj != null) && !(dependencyObj is TControl))
            {
                dependencyObj = VisualTreeHelper.GetParent(dependencyObj);
            }

            return dependencyObj as TControl;  // Note, null as Class is val 
        }

        /// <summary>
        /// Gets the first log item of the current data grid view.
        /// </summary>
        private object GetFirstRow()
        {
            var ele = dataGridLogEntries.InputHitTest(new Point(5, 50));
            var row = FindAncestorControl<DataGridRow>(ele as DependencyObject);
            Debug.WriteLine($"FindRowByPoint(5, 50) returns {row}");
            if (null == row)
            {
                return null;
            }

            int itemIndex = dataGridLogEntries.ItemContainerGenerator.IndexFromContainer(row);
            if (itemIndex < 0)
            {
                Debug.WriteLine($"Find first IndexFromContainer returns {itemIndex} ");
                return null;
            }
            else
            {
                var item = dataGridLogEntries.Items[itemIndex];
                Debug.WriteLine($"Find first row returns object {item.ToString()}");
                return item;
            }
        }

        /// <summary>
        /// Response to data grid scroll change event.
        /// Auto load more logs when it scrolls down to bottom.
        /// </summary>
        private void dataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            ScrollViewer sv = e.OriginalSource as ScrollViewer;
            if (sv == null)
            {
                return;
            }

            ViewModel?.OnFirstRowChanged(GetFirstRow());

            if (e.VerticalOffset == sv.ScrollableHeight)
            {
                Debug.WriteLine("Now it is at bottom");
                ViewModel?.LoadNextPage();
            }
        }

        /// <summary>
        /// When mouse click on a row, toggle display the row detail.
        /// 
        /// Note, it is necessay to find cell before find row. 
        /// Otherwise when clicking at the detail view area, it 'finds' the DataGridRow too.
        /// </summary>
        private void dataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var cell = FindAncestorControl<DataGridCell>(e.OriginalSource as DependencyObject);
            DataGridRow row = FindAncestorControl<DataGridRow>(cell);
            if (row != null)
            {
                row.DetailsVisibility = 
                    row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}