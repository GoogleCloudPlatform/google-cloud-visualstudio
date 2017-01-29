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
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Custom control for time input.
    /// </summary>
    [TemplatePart(Name = "PART_HourBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_MinuteBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_SecondBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_UpArrow", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_DownArrow", Type = typeof(RepeatButton))]
    public class TimeBox : Control
    {
        /// <summary>
        /// Define a TextBox, Dependecies pair. 
        /// TODO: use named Tuple in C# 7
        /// </summary>
        private class TextBoxDependencyPropertyPair
        {
            /// <summary>
            /// Gets the time part <seealso cref="System.Windows.Controls.TextBox"/>
            /// </summary>
            public TextBox TextBox { get; }

            /// <summary>
            /// Gets the <seealso cref="DependencyProperty"/> that is associated to the time part TextBox.
            /// </summary>
            public DependencyProperty Property { get; }

            /// <summary>
            /// Initializes the object.
            public TextBoxDependencyPropertyPair(TextBox box, DependencyProperty property)
            {
                TextBox = box;
                Property = property;
            }
        }

        private RepeatButton _upButton, _downButton;
        private TextBox _hourBox, _minuteBox, _secondBox;
        private ComboBox _timeTypeCombo;
        private List<TextBoxDependencyPropertyPair> _timePartsBoxes;

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(
                "Time",
                typeof(TimeSpan),
                typeof(TimeBox),
                new FrameworkPropertyMetadata(default(TimeSpan), OnTimePropertyChanged, null)
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                },
                ValidateTimeSpan);

        public static readonly DependencyProperty HourProperty =
            DependencyProperty.Register(
                "Hour",
                typeof(int),
                typeof(TimeBox),
                new FrameworkPropertyMetadata(0, OnTimePartPropertyChanged, null),
                ValidateHour);

        public static readonly DependencyProperty MinuteProperty =
            DependencyProperty.Register(
                "Minute",
                typeof(int),
                typeof(TimeBox),
                new FrameworkPropertyMetadata(0, OnTimePartPropertyChanged, null),
                ValidateMinute);

        public static readonly DependencyProperty SecondProperty =
            DependencyProperty.Register(
                "Second",
                typeof(int),
                typeof(TimeBox),
                new FrameworkPropertyMetadata(0, OnTimePartPropertyChanged, null),
                ValidateSecond);

        /// <summary>
        /// The hour value for hour textbox.
        /// </summary>
        public int Hour
        {
            get { return (int)GetValue(HourProperty); }
            set { SetValue(HourProperty, value); }
        }

        /// <summary>
        /// The minute value for minute textbox.
        /// </summary>
        public int Minute
        {
            get { return (int)GetValue(MinuteProperty); }
            set { SetValue(MinuteProperty, value); }
        }

        /// <summary>
        /// The second value for second textbox.
        /// </summary>
        public int Second
        {
            get { return (int)GetValue(SecondProperty); }
            set { SetValue(SecondProperty, value); }
        }

        /// <summary>
        /// The aggregated time value.
        /// </summary>
        public TimeSpan Time
        {
            get { return (TimeSpan)GetValue(TimeProperty); }
            set { SetValue(TimeProperty, value); }
        }

        static TimeBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeBox), new FrameworkPropertyMetadata(typeof(TimeBox)));
        }

        /// <summary>
        /// Initialize the control template.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _upButton = Template.FindName("PART_UpArrow", this) as RepeatButton;
            _downButton = Template.FindName("PART_DownArrow", this) as RepeatButton;
            _hourBox = Template.FindName("PART_HourBox", this) as TextBox;
            _minuteBox = Template.FindName("PART_MinuteBox", this) as TextBox;
            _secondBox = Template.FindName("PART_SecondBox", this) as TextBox;
            _timePartsBoxes = new List<TextBoxDependencyPropertyPair>(
                new TextBoxDependencyPropertyPair[] {
                new TextBoxDependencyPropertyPair(_hourBox, HourProperty),
                new TextBoxDependencyPropertyPair(_minuteBox, MinuteProperty),
                new TextBoxDependencyPropertyPair(_secondBox, SecondProperty)
            });
            _timeTypeCombo = Template.FindName("PART_TimeType", this) as ComboBox;
            if (_upButton != null && _downButton != null)
            {
                _upButton.Click += RepeatButton_Click;
                _downButton.Click += RepeatButton_Click;
            }
            foreach (var tuple in _timePartsBoxes)
            {
                if (tuple.TextBox != null)
                {
                    tuple.TextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                    tuple.TextBox.PreviewTextInput += TextBox_PreviewTextInput;
                }
            }
        }

        private static void OnTimePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            TimeBox control = source as TimeBox;
            TimeSpan newValue = (TimeSpan)e.NewValue;
            TimeSpan oldValue = (TimeSpan)e.OldValue;
            // SetTimeParts triggers OnTimePartPropertyChanged
            // And it sets control.Time that calls back to current method OnTimePropertyChanged.
            // This must be checked to avoid such a deadloop. 
            if (newValue != oldValue)
            {
                control.SetTimeParts(newValue);
            }
        }

        private static void OnTimePartPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            TimeBox control = source as TimeBox;
            int newValue = (int)e.NewValue;
            int oldValue = (int)e.OldValue;
            // Setting control.Time triggers OnTimePropertyChanged
            // And SetTimePars inside OnTimePropertyChanged  may call back to current method OnTimePartPropertyChanged.
            // This must be checked to avoid such a deadloop. 
            if (newValue != oldValue)
            {
                control.Time = control.ComposeTime();
            }
        }

        /// <summary>
        /// Compose the hour, minute, second into <seealso cref="TimeSpan"/>
        /// </summary>
        private TimeSpan ComposeTime()
        {
            return new TimeSpan(Hour, Minute, Second);
        }

        /// <summary>
        /// Set time parts value when <seealso cref="TimeProperty"/> changes.
        /// Checking if the time parts value differ is critical to avoid deadloop.
        /// Without doing so, OnTimePartPropertyChanged and OnTimePropertyChanged may call to each other indefinitely. 
        /// </summary>
        private void SetTimeParts(TimeSpan time)
        { 
            if (Hour != time.Hours)
            {
                Hour = time.Hours;
            }

            if (Minute != time.Minutes)
            {
                Minute = time.Minutes;
            }

            if (Second != time.Seconds)
            {
                Second = time.Seconds;
            }
        }

        /// <summary>
        /// Gets the time part input text box index at <seealso cref="_timePartsBoxes"/> array.
        /// </summary>
        private int GetTimePartTextBoxesIndex(object sender)
        {
            return _timePartsBoxes.FindIndex(x => x.TextBox == sender);
        }

        /// <summary>
        /// Disables non-digits input.
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!StringUtils.IsDigitsOnly(e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        private void UpDownPropertyValue(DependencyProperty dp, bool increase)
        {
            int min = 0;
            int max = dp == HourProperty ? 23 : 59;

            int oldValue = (int)GetValue(dp);
            int newValue = increase ? oldValue + 1 : oldValue - 1;
            if (newValue > max)
            {
                newValue = min;
            }
            if (newValue < min)
            {
                newValue = max;
            }

            SetValue(dp, newValue);
        }

        /// <summary>
        /// Response to RepeatButton click event that updates the time part input box value.
        /// </summary>
        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            DependencyProperty dp = HourProperty;
            if (_minuteBox.IsFocused)
            {
                dp = MinuteProperty;
            }
            else if (_secondBox.IsFocused)
            {
                dp = SecondProperty;
            }

            UpDownPropertyValue(dp, e.Source == _upButton);
        }

        /// <summary>
        /// There are three boxes in the time control. Hour, Minute, Second.
        /// (1) Left arrow key move to the prior box, if the caet is at the beginning of the input text.
        /// (2) Right arrow key move to the next box, if the caret is at the end of the input text.
        /// (3) Press Enter key moves to the next box.
        /// (4) Down arrow key increases the box value.
        /// (5) Up Arrow Key decreases the box value.
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.IsDown)
            {
                return;
            }

            var textBox = sender as TextBox;
            int index = GetTimePartTextBoxesIndex(sender);
            if (index < 0)
            {
                return;
            }

            // Down arrow key, decrease the part value.
            if (e.Key == Key.Down)
            {
                UpDownPropertyValue(_timePartsBoxes[index].Property, increase: false);
            }
            // Up arrow key, increase the part value.
            else if (e.Key == Key.Up)
            {
                UpDownPropertyValue(_timePartsBoxes[index].Property, increase: true);
            }            // Enter key, move focus to the next box.
            else if (e.Key == Key.Enter && index < 2)
            {
                _timePartsBoxes[index + 1].TextBox.Focus();
            }
            // Left arrow, if the caret is at the begining of the current text box, move to the prior box.
            else if (e.Key == Key.Left && textBox.CaretIndex == 0 && index > 0)
            {
                _timePartsBoxes[index - 1].TextBox.Focus();
            }
            // Right arrow, if caret is at the end of the current text box, move to the next box.
            else if (e.Key == Key.Right && textBox.CaretIndex == textBox.Text.Length && index < 2)
            {
                _timePartsBoxes[index + 1].TextBox.Focus();
            }
        }

        private static bool ValidateHour(object value)
        {
            int t = (int)value;
            return !(t < 0 || t > 23);
        }

        private static bool ValidateMinute(object value)
        {
            int t = (int)value;
            return !(t < 0 || t > 60);
        }

        private static bool ValidateSecond(object value)
        {
            int t = (int)value;
            return !(t < 0 || t > 60);
        }

        private static bool ValidateTimeSpan(object value)
        {
            TimeSpan time = (TimeSpan)value;
            return time.TotalDays < 1;
        }
    }

}
