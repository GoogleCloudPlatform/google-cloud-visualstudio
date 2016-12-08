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
        /// <summary>
        /// Used to manually turn off debug messagge of this file. 
        /// </summary>
        private bool _turnOnDebugWriteLine = false;

        /// <summary>
        /// Sets or resets the control with a new ViewModel object.
        /// </summary>
        public LogsViewerViewModel ViewModel
        {
            private get
            {
                return DataContext as LogsViewerViewModel;
            }

            set
            {
                Debug.Assert(value is ViewModelBase);
                DataContext = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsViewerToolWindowControl"/> class.
        /// </summary>
        public LogsViewerToolWindowControl()
        {
            this.InitializeComponent();
        }


        private void DebugWriteLine(string traceLine)
        {
            if (_turnOnDebugWriteLine)
            {
                Debug.WriteLine(traceLine);
            }
        }

        private DataGridRow preSelected;
        private int preSelectedRowInex;

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (dataGridLogEntries.SelectedIndex >= 0)
            {
                DataGridRow row = (DataGridRow)dataGridLogEntries.ItemContainerGenerator.ContainerFromIndex(dataGridLogEntries.SelectedIndex);
                preSelected = row;
                preSelectedRowInex = dataGridLogEntries.SelectedIndex;
            }


            if (_turnOnDebugWriteLine)
            {
                Debug.WriteLine($"dg_selectionchanged {dataGridLogEntries.SelectedIndex}");
            }

            // This is necessary to fix:
            // By default DataGrid opens detail view on selected row. 
            // It automatically opens detail view on mouse move
            DebugWriteLine($"dg_selectionchanged UnselectAll");
            dataGridLogEntries.UnselectAll();
        }

        private void dataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;


            DataGridCell cell = dep as DataGridCell;
            // do something

            // navigate further up the tree
            while ((dep != null) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            DataGridRow row = dep as DataGridRow;
            if (row != null)
            {
                row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private DataGridRow SelectedRow()
        {
            return (DataGridRow)dataGridLogEntries.ItemContainerGenerator.ContainerFromIndex(dataGridLogEntries.SelectedIndex);
        }

        private DataGridRow previousHighlighted;

        private DataGridRow MouseOverRow(MouseEventArgs e)
        {
            // Use HitTest to resolve the row under the cursor
            var ele = dataGridLogEntries.InputHitTest(e.GetPosition(null));
            if (_turnOnDebugWriteLine)
            {
                Debug.WriteLine($"InputHitTest element {ele?.GetType()}");
            }
            return ele as DataGridRow;
        }

        /// <summary>
        /// Somehow this is neccessary to change the seleted item
        /// Otherwise the "SelectedItem" become white blank.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("dg_MouseMove Unselected All");
            //dg.UnselectAll();

            // MouseOverRow(e);

            if (true)
            {
                DependencyObject dep = (DependencyObject)e.OriginalSource;

                // iteratively traverse the visual tree
                while ((dep != null) && !(dep is DataGridCell))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (dep == null)
                    return;


                DataGridCell cell = dep as DataGridCell;
                // do something

                // navigate further up the tree
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                DataGridRow row = dep as DataGridRow;
                if (row != null)
                {
                    int rowIndex = dataGridLogEntries.ItemContainerGenerator.IndexFromContainer(row);
                    if (_turnOnDebugWriteLine)
                    {
                        Debug.WriteLine($"Set Selected to row {rowIndex}, previous selected {dataGridLogEntries.SelectedIndex} ");
                    }
                    if (previousHighlighted != row)
                    {
                        previousHighlighted?.InvalidateVisual();
                        previousHighlighted = row;
                    }

                    if (preSelected != row)
                    {
                        DebugWriteLine($"pre selected row {preSelectedRowInex} {preSelected}");
                        preSelected?.InvalidateVisual();
                        preSelected?.InvalidateMeasure();
                    }

                    object item = dataGridLogEntries.Items[rowIndex];
                    ViewModel.SetSelectedChanged(item);
                    dataGridLogEntries.SelectedItem = item;
                }
            }
        }

        private void dtGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var grid = sender as DataGrid;

            ScrollViewer sv = e.OriginalSource as ScrollViewer;
            if (sv == null)
            {
                Debug.Assert(false);
                return;
            }

            DebugWriteLine($"e.VerticalOffset={e.VerticalOffset}, ScrollableHeight={sv.ScrollableHeight}, e.VerticalChange={e.VerticalChange}, e.ViewportHeight={e.ViewportHeight}");
            if (e.VerticalOffset == sv.ScrollableHeight)
            {
                DebugWriteLine("Now it is at bottom");
                ViewModel?.LoadNextPage();
            }
        }
    }
}