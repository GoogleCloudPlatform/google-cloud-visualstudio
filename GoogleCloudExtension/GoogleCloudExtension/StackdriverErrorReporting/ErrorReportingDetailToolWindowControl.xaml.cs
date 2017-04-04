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
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Interaction logic for ErrorReportingDetailToolWindowControl.
    /// </summary>
    public partial class ErrorReportingDetailToolWindowControl : UserControl
    {
        private ErrorReportingDetailViewModel ViewModel => DataContext as ErrorReportingDetailViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReportingDetailToolWindowControl"/> class.
        /// </summary>
        public ErrorReportingDetailToolWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When mouse click on a row, toggle display the row detail.
        /// if the mouse is clikcing on detail panel, does not collapse it.
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

        /// <summary>
        /// This is necessary workaround to enable outer scroll bar.
        /// </summary>
        private void dataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}