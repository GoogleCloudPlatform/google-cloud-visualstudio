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
        /// On Windows8, Windows10, the combobox backgroud property does not work.
        /// This is a workaround to fix the problem.
        /// </summary>
        private void ComboBox_Loaded(Object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var comboBoxTemplate = comboBox.Template;
            var toggleButton = comboBoxTemplate.FindName("toggleButton", comboBox) as ToggleButton;
            var toggleButtonTemplate = toggleButton.Template;
            var border = toggleButtonTemplate.FindName("templateRoot", toggleButton) as Border;
            var backgroud = comboBox.Background;
            border.Background = backgroud;
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

            if (e.VerticalOffset == sv.ScrollableHeight)
            {
                Debug.WriteLine("Now it is at bottom");
                ViewModel?.LoadNextPage();
            }
        }

        /// <summary>
        /// When mouse clicks on a row, toggle display the row detail.
        /// If the mouse is clikcing on detail panel, does not collapse it.        
        /// </summary>
        private void dataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var dependencyObj = e.OriginalSource as DependencyObject;
            DataGridRow row = DataGridUtils.FindAncestorControl<DataGridRow>(dependencyObj);
            if (row != null)
            {
                if (null == DataGridUtils.FindAncestorControl<DataGridDetailsPresenter>(dependencyObj))
                {
                    row.DetailsVisibility =
                        row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }
    }
}