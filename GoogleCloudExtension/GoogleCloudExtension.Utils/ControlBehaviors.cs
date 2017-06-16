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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class defines general control behaviors implemented as attached <seealso cref="DependencyProperty"/> instances.
    /// </summary>
    public static class ControlBehaviors
    {
        #region Double click command.

        /// <summary>
        /// This attached property transforms the <seealso cref="Control.MouseDoubleClick" /> event into a
        /// <see cref="ICommand"/> invokation. This makes it possible to implement the necessary code in the view model.
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached(
                "DoubleClickCommand",
                typeof(ICommand),
                typeof(ControlBehaviors),
                new PropertyMetadata(OnDoubleClickCommandPropertyChanged));

        /// <summary>
        /// The getter for the attached property.
        /// </summary>
        public static ICommand GetDoubleClickCommand(Control self) => (ICommand)self.GetValue(DoubleClickCommandProperty);

        /// <summary>
        /// The setter for the attached property.
        /// </summary>
        public static void SetDoubleClickCommand(Control self, ICommand value)
        {
            self.SetValue(DoubleClickCommandProperty, value);
        }

        private static void OnDoubleClickCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (Control)d;

            if (e.OldValue != null && e.NewValue == null)
            {
                self.MouseDoubleClick -= OnControlDoubleClick;
            }

            if (e.NewValue != null && e.OldValue == null)
            {
                self.MouseDoubleClick += OnControlDoubleClick;
            }
        }

        private static void OnControlDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var self = (Control)sender;

            var command = GetDoubleClickCommand(self);
            var parameter = GetDoubleClickCommandParameter(self);

            if (!command.CanExecute(parameter))
            {
                return;
            }
            command.Execute(parameter);
        }

        #endregion

        #region Double click command paramater.

        /// <summary>
        /// This attached property will contain the parameter to be passed to the command stored in 
        /// <seealso cref="DoubleClickCommandProperty"/>. It can be any object.
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "DoubleClickCommandParameter",
                typeof(object),
                typeof(ControlBehaviors));

        /// <summary>
        /// The getter for the <seealso cref="DoubleClickCommandParameterProperty"/>.
        /// </summary>
        /// <param name="self">The <seealso cref="Control"/>.</param>
        public static object GetDoubleClickCommandParameter(Control self) =>
            self.GetValue(DoubleClickCommandParameterProperty);

        /// <summary>
        /// The setter for the <seealso cref="DoubleClickCommandParameterProperty"/>.
        /// </summary>
        /// <param name="self">The <seealso cref="Control"/></param>
        /// <param name="value">The new value for the property.</param>
        public static void SetDoubleClickCommandParameter(Control self, object value)
        {
            self.SetValue(DoubleClickCommandParameterProperty, value);
        }

        #endregion 
    }
}
