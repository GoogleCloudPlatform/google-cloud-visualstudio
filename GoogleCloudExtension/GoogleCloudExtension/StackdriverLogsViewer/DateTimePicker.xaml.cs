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

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Interaction logic for LogDateTimePicker.xaml
    /// </summary>
    public partial class DateTimePicker : UserControl
    {
        /// <summary>
        /// Inistantialize an instance of <seealso cref="DateTimePicker"/> class.
        /// </summary>
        public DateTimePicker()
        {
            InitializeComponent();
            _comboPickTime.SelectedIndex = 0;
        }

        /// <summary>
        /// Wpf calendar holds the mouse clicking event.
        /// Once focus is inside the calendar control, clicking outside won't respond.
        /// This is a workaround to fix the problem.
        /// For more information, refer to:
        /// http://stackoverflow.com/questions/6024372/wpf-calendar-control-holding-on-to-the-mouse
        /// </summary>
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            if (Mouse.Captured is Calendar || Mouse.Captured is CalendarItem)
            {
                Mouse.Capture(null);
            }
        }
    }
}
