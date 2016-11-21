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
using System.Windows.Input;

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// Contains the information necessary to create a button in the dialog. This class is a
    /// <seealso cref="DependencyObject"/> to allow bindings to be set on the properties. Specially 
    /// usefull for the <seealso cref="Command"/> property.
    /// </summary>
    public class DialogButtonInfo : FrameworkElement
    {
        #region DependencyProperty registrations

        // These are all of the DependencyProperty registrations for the properties in the object.

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(
                nameof(Caption),
                typeof(string),
                typeof(DialogButtonInfo));

        public static readonly DependencyProperty IsDefaultProperty =
            DependencyProperty.Register(
                nameof(IsDefault),
                typeof(bool),
                typeof(DialogButtonInfo));

        public static readonly DependencyProperty IsCancelProperty =
            DependencyProperty.Register(
                nameof(IsCancel),
                typeof(bool),
                typeof(DialogButtonInfo));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(DialogButtonInfo));

        #endregion

        /// <summary>
        /// The caption to use for the button.
        /// </summary>
        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); ; }
            set { SetValue(CaptionProperty, value); }
        }

        /// <summary>
        /// Whether this button is the default button, invoked with the enter key.
        /// </summary>
        public bool IsDefault
        {
            get { return (bool)GetValue(IsDefaultProperty); }
            set { SetValue(IsDefaultProperty, value); }
        }

        /// <summary>
        /// Whether this button is the cancel button, invoked with the ESC key.
        /// </summary>
        public bool IsCancel
        {
            get { return (bool)GetValue(IsCancelProperty); }
            set { SetValue(IsCancelProperty, value); }
        }

        /// <summary>
        /// The command to execute when the button is invoked.
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
    }
}