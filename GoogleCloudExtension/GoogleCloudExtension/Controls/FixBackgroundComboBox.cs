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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// A customized ComboBox.
    /// This is to override the Loaded event so as to set the combo box background correctly.
    /// </summary>
    public class FixBackgroundComboBox : ComboBox
    {
        /// <summary>
        /// Instantiate an new instance of <seealso cref="FixBackgroundComboBox"/> class.
        /// </summary>
        public FixBackgroundComboBox()
        {
            Loaded += OnComboBoxLoaded;
        }

        /// <summary>
        /// On Windows8, Windows10, the combobox background property does not work.
        /// This is a workaround to fix the problem.
        /// </summary>
        private void OnComboBoxLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var comboBoxTemplate = comboBox.Template;
            if (!(comboBoxTemplate.FindName("toggleButton", comboBox) is ToggleButton toggleButton))
            {
                return;
            }
            ControlTemplate toggleButtonTemplate = toggleButton.Template;
            var border = (Border)toggleButtonTemplate.FindName("templateRoot", toggleButton);
            Brush background = comboBox.Background;
            border.Background = background;
        }
    }
}
