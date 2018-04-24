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

using GoogleCloudExtension.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Interaction logic for ErrorReportingDetailToolWindowControl.
    /// </summary>
    public partial class ErrorReportingDetailToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReportingDetailToolWindowControl"/> class.
        /// </summary>
        public ErrorReportingDetailToolWindowControl()
        {
            InitializeComponent();
        }

        private void DeselectSelectedTargetRow(object sender, MouseButtonEventArgs e)
        {
            var cell = DataGridUtils.FindAncestorControl<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell != null && cell.IsSelected)
            {
                var grid = DataGridUtils.FindAncestorControl<DataGrid>(cell);
                if (grid != null)
                {
                    switch (grid.SelectionUnit)
                    {
                        case DataGridSelectionUnit.Cell:
                        case DataGridSelectionUnit.CellOrRowHeader:
                            cell.IsSelected = false;
                            break;
                        case DataGridSelectionUnit.FullRow:
                            grid.UnselectAllCells();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void ErrorReportingDetailToolWindowControl_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = DataContext as ErrorReportingDetailViewModel;
            if (viewModel != null)
            {
                viewModel.IsVisibleUnbound = (bool) e.NewValue;
            }
        }
    }
}