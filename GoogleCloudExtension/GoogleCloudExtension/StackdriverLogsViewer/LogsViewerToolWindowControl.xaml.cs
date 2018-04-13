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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
            PackageUtils.ReferenceType(typeof(VisibilityConverter));
            InitializeComponent();
        }

        /// <summary>
        /// Response to data grid scroll change event.
        /// Auto load more logs when it scrolls down to bottom.
        /// </summary>
        private void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer;
            if (sv == null || !sv.IsMouseOver)
            {
                return;
            }

            if (e.VerticalOffset > 0 && e.VerticalOffset == sv.ScrollableHeight)
            {
                Debug.WriteLine($"Now scrollbar is at bottom. {sv.VerticalOffset}, {sv.ScrollableHeight}");
                ViewModel?.LoadNextPage();
            }
        }

        /// <summary>
        /// Implements the <see cref="ApplicationCommands.Copy"/> routed event on a details TreeView.
        /// Executes the CopyCommand of the selected ObjectNodeTree item.
        /// </summary>
        private void DetailsTreeView_OnCopy(object sender, ExecutedRoutedEventArgs e)
        {
            var detailsTreeView = sender as TreeView;
            var treeNodeViewModel = detailsTreeView?.SelectedItem as ObjectNodeTree;
            treeNodeViewModel?.CopyCommand.Execute(null);
        }

        private void LogsViewerToolWindowControl_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = DataContext as LogsViewerViewModel;
            if (viewModel != null)
            {
                viewModel.IsVisibleUnbound = (bool)e.NewValue;
            }
        }
    }
}
