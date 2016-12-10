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

        /// <summary>
        /// On Windows8, the combobox backgroud property does not work.
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
        /// By default DataGrid opens detail view on selected row.
        /// This is a workaround to fix the problem:
        /// </summary>
        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DebugWriteLine($"dg_selectionchanged {dataGridLogEntries.SelectedIndex}");
            dataGridLogEntries.UnselectAll();
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