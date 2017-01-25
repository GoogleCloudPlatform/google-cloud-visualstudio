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

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Custom control for time input.
    /// </summary>
    public partial class TimeBox : UserControl
    {
        private TextBox[] TextBoxParts => new TextBox[] { hourBox, minuteBox, secondBox };

        /// <summary>
        /// Gets the view model
        /// </summary>
        private TimeBoxViewModel ViewModel
        {
            get { return DataContext as TimeBoxViewModel; }
        }

        /// <summary>
        /// Instantializes a new instance of <seealso cref="TimeBox"/> class.
        /// </summary>
        public TimeBox()
        {
            this.InitializeComponent();

            _upButton.Click += RepeatButton_Click;
            _downButton.Click += RepeatButton_Click;
            foreach (var textBox in TextBoxParts)
            {
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
            }
        }

        /// <summary>
        /// Gets the time part input text box index at TextBoxParts array.
        /// </summary>
        private int GetSenderIndex(object sender)
        {
            for (int i = 0; i < TextBoxParts.Length; ++i)
            {
                if (TextBoxParts[i] == sender as TextBox)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Check if input is digits only.
        /// </summary>
        private static bool IsTextAllowed(string text)
        {
            foreach (var chr in text)
            {
                if (chr < '0' || chr > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Disables non-digits input.
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsTextAllowed(e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        /// <summary>
        /// Response to RepeatButton click event that updates the time part input box value.
        /// </summary>
        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            bool isUpButton = e.Source == this._upButton;
            if (minuteBox.IsFocused)
            {
                ViewModel.Minute = isUpButton ? ViewModel.Minute + 1 : ViewModel.Minute - 1;
            }
            else if (secondBox.IsFocused)
            {
                ViewModel.Second = isUpButton ? ViewModel.Second + 1 : ViewModel.Second - 1;
            }
            else
            {
                ViewModel.Hour = isUpButton ? ViewModel.Hour + 1 : ViewModel.Hour - 1;
            }
        }

        /// <summary>
        /// Capture left, right key etc that moves focus easily.
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

            if ((e.Key == Key.Down || e.Key == Key.Enter) && index < 2)
            {
                TextBoxParts[index + 1].Focus();
            }

            if (e.Key == Key.Up && index > 0)
            {
                TextBoxParts[index - 1].Focus();
            }

            if (e.Key == Key.Left && textBox.CaretIndex == 0)
            {
                if (index > 0)
                {
                    TextBoxParts[index - 1].Focus();
                }
            }

            if (e.Key == Key.Right && textBox.CaretIndex == textBox.Text.Length)
            {
                if (index < 2)
                {
                    TextBoxParts[index + 1].Focus();
                }
            }
        }
    }
}
