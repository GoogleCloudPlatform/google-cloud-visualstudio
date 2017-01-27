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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// A customized ComboBox.
    /// This is to override the Loaded event so as to set the combo box backgroud correctly.
    /// </summary>
    public class FixBackgroundComboBox : ComboBox
    {
        /// <summary>
        /// Inistantialize an new instance of <seealso cref="FixBackgroundComboBox"/> class.
        /// </summary>
        public FixBackgroundComboBox() : base()
        {
            this.Loaded += OnComboBoxLoaded;
        }

        /// <summary>
        /// On Windows8, Windows10, the combobox backgroud property does not work.
        /// This is a workaround to fix the problem.
        /// </summary>
        private void OnComboBoxLoaded(Object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var comboBoxTemplate = comboBox.Template;
            var toggleButton = comboBoxTemplate.FindName("toggleButton", comboBox) as ToggleButton;
            if (toggleButton == null)
            {
                return;
            }
            var toggleButtonTemplate = toggleButton.Template;
            var border = toggleButtonTemplate.FindName("templateRoot", toggleButton) as Border;
            var backgroud = comboBox.Background;
            border.Background = backgroud;
        }
    }
}
