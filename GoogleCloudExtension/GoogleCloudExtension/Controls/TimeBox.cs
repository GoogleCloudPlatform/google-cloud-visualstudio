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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Define AM/PM as enums.
    /// </summary>
    [Serializable]
    public enum TimeType { AM, PM }

    /// <summary>
    /// Custom control for time input.
    /// </summary>
    [TemplatePart(Name = "PART_HourBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_MinuteBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_SecondBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_UpArrow", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_DownArrow", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_TimeType", Type = typeof(ComboBox))]
    public class TimeBox : Control
    {
        private RepeatButton _upButton, _downButton;
        private TextBox _hourBox, _minuteBox, _secondBox;
        private ComboBox _timeTypeCombo;
        private TextBox[] TimeParts => new TextBox[] { _hourBox, _minuteBox, _secondBox };

        public static readonly DependencyProperty TimeTypeProperty =
            DependencyProperty.Register("TimeType",
                typeof(TimeType),
                typeof(TimeBox),
                new FrameworkPropertyMetadata(TimeType.AM, OnTimePartPropertyChanged, null));

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
        /// AM or PM Combobox value.
        /// </summary>
        public TimeType TimeType
        {
            get { return (TimeType)GetValue(TimeTypeProperty); }
            set { SetValue(TimeTypeProperty, value); }
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
            _timeTypeCombo = Template.FindName("PART_TimeType", this) as ComboBox;
            _upButton.Click += RepeatButton_Click;
            _downButton.Click += RepeatButton_Click;
            foreach (var textBox in TimeParts)
            {   
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
            }
        }

        private static void OnTimePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            TimeBox control = source as TimeBox;
            TimeSpan newValue = (TimeSpan)e.NewValue;
            TimeSpan oldValue = (TimeSpan)e.OldValue;
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
            var h = Hour;
            var hour = TimeType == TimeType.AM ? h : (h % 12) + 12;
            return new TimeSpan(hour, Minute, Second);
        }

        /// <summary>
        /// Set time parts value when <seealso cref="TimeProperty"/> changes.
        /// </summary>
        private void SetTimeParts(TimeSpan time)
        { 
            TimeType ampm = TimeType.AM;
            var h = time.Hours;
            if (h >= 12)
            {
                h = time.Hours - 12;
                ampm = TimeType.PM;
            }

            if (h == 0)
            {
                h = 12;
            }

            if (ampm != TimeType)
            {
                TimeType = ampm;
            }

            if (Hour != h)
            {
                Hour = h;
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
        /// Gets the time part input text box index at TextBoxParts array.
        /// </summary>
        private int GetSenderIndex(object sender)
        {
            for (int i = 0; i < TimeParts.Length; ++i)
            {
                if (TimeParts[i] == sender as TextBox)
                {
                    return i;
                }
            }

            return -1;
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
            int max = 59, min = 0;
            if (dp == HourProperty)
            {
                max = 12;
                min = 1;
            }

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
        /// (1) Left, Up arrow keys move to the prior box.
        /// (2) Right, Down arrow keys move to the next box.
        /// (3) Press Enter key moves to the next box.
        /// (4) Input third digits at a box will automatically take it to the next box.
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.IsDown)
            {
                return;
            }

            var textBox = sender as TextBox;
            int index = GetSenderIndex(sender);
            if (index < 0)
            {
                return;
            }

            // Down arrow, Enter key, move focus to the next box.
            if ((e.Key == Key.Down || e.Key == Key.Enter) && index < 2)
            {
                TimeParts[index + 1].Focus();
            }

            // Up arrow, move focus to the prior box.
            if (e.Key == Key.Up && index > 0)
            {
                TimeParts[index - 1].Focus();
            }

            // Left arrow, if the caret is at the begining of the current text box, move to the prior box.
            if (e.Key == Key.Left && textBox.CaretIndex == 0 && index > 0)
            {
                TimeParts[index - 1].Focus();
            }

            // Right arrow, if caret is at the end of the current text box, move to the next box.
            if (e.Key == Key.Right && textBox.CaretIndex == textBox.Text.Length && index < 2)
            {
                TimeParts[index + 1].Focus();
            }
        }

        private static bool ValidateHour(object value)
        {
            int t = (int)value;
            return !(t < 0 || t > 12);
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
