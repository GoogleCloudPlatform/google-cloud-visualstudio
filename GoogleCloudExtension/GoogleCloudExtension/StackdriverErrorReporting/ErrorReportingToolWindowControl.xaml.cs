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

using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources.ErrorReporting;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Interaction logic for ErrorReportingToolWindowControl.
    /// </summary>
    public partial class ErrorReportingToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReportingToolWindowControl"/> class.
        /// </summary>
        public ErrorReportingToolWindowControl()
        {
            this.InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DataContext = ErrorReportingViewModel.Instance;
            var viewModel = DataContext as ErrorReportingViewModel;
            autoReloadToggleButton.AutoReload += (sender, e) => viewModel.Refresh();
            CredentialsStore.Default.CurrentProjectIdChanged += (sender, e) =>
            {
                SerDataSourceInstance.RecreateSourceInstance();
                viewModel.Refresh();
            };
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
                var viewModel = DataContext as ErrorReportingViewModel;
                viewModel?.LoadNextPage();
            }
        }
    }
}    