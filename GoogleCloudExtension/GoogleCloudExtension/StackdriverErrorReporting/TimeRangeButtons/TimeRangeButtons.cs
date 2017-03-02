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
    /// Define time range selection control.
    /// It is a list of buttons with time range as caption. 
    /// User can click on button to select/change the time range.
    /// </summary>
    public class TimeRangeButtons : Selector
    {
        public static readonly DependencyProperty OnItemSelectedCommandProperty =
            DependencyProperty.Register(
                nameof(OnItemSelectedCommand),
                typeof(ICommand),
                typeof(TimeRangeButtons),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The command that respond to the range item button click event.
        /// </summary>
        public ICommand OnItemSelectedCommand
        {
            get { return (ICommand)GetValue(OnItemSelectedCommandProperty); }
            set { SetValue(OnItemSelectedCommandProperty, value); }
        }

        /// <summary>
        /// Override the <seealso cref="OnApplyTemplate"/> method to initialize controls.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            OnItemSelectedCommand = new ProtectedCommand<TimeRangeItem>((item) => SelectedItem = item);
            SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.RemovedItems)
            {
                (item as TimeRangeItem).IsCurrentSelection = false;
            }
            foreach (var item in e.AddedItems)
            {
                (item as TimeRangeItem).IsCurrentSelection = true;
            }
        }
    }
}
