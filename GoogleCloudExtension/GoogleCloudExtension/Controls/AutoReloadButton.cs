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

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Define an auto reload button.
    /// When it is enabled, the button uses a timer to trigger reload event.
    /// </summary>
    public class AutoReloadButton : ImageToggleButton
    {
        // TODO: proper dispose of the timer.
        private readonly DispatcherTimer _timer;

        static AutoReloadButton()
        {
            // Override IsChecked property to add a handler when the state changes.
            ToggleButton.IsCheckedProperty.OverrideMetadata(
                typeof(AutoReloadButton), 
                new FrameworkPropertyMetadata(false, OnIsCheckedChanged));
        }

        public static DependencyProperty AutoReloadCommandProperty =
            DependencyProperty.Register(
                nameof(AutoReloadCommand),
                typeof(ICommand),
                typeof(AutoReloadButton));

        public static DependencyProperty IntervalSecondsProperty =
            DependencyProperty.Register(
                nameof(IntervalSeconds),
                typeof(int),
                typeof(AutoReloadButton),
                new FrameworkPropertyMetadata(10, OnIntervalSecondsChanged));

        /// <summary>
        /// Gets or sets the command that responds to auto reload timer event.
        /// </summary>
        public ICommand AutoReloadCommand
        {
            get { return (ICommand)GetValue(AutoReloadCommandProperty); }
            set { SetValue(AutoReloadCommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets the interval in seconds that auto reload happens.
        /// </summary>
        public int IntervalSeconds
        {
            get { return (int)GetValue(IntervalSecondsProperty); }
            set { SetValue(IntervalSecondsProperty, value); }
        }

        public AutoReloadButton()
        {
            _timer = new DispatcherTimer();
        }

        /// <summary>
        /// Initialize the control template.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //  DispatcherTimer setup
            _timer.Interval = TimeSpan.FromSeconds(IntervalSeconds);
            _timer.Tick += (sender, e) => ExecuteAutoReloadCommand();
        }

        private static void OnIntervalSecondsChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            // WPF framework makes the call and value type is int. 
            // Does not need to check the type.
            int newIntervalSeconds = (int)e.NewValue;
            AutoReloadButton button = source as AutoReloadButton;
            button._timer.Interval = TimeSpan.FromSeconds(newIntervalSeconds);
        }

        private static void OnIsCheckedChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            bool isChecked = (bool)e.NewValue;
            AutoReloadButton button = source as AutoReloadButton;
            if (isChecked)
            {
                button._timer.Start();
                button.ExecuteAutoReloadCommand();
            }
            else
            {
                button._timer.Stop();
            }
        }

        private void ExecuteAutoReloadCommand()
        {
            if (AutoReloadCommand == null || !AutoReloadCommand.CanExecute(null))
            {
                return;
            }
            AutoReloadCommand.Execute(null);
        }
    }
}
