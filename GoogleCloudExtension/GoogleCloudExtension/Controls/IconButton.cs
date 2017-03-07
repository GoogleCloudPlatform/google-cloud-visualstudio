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
using System.Windows.Media;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// This class represents a specialization of the button control that only shows icons, showing
    /// different icons depending on the visual state.
    /// </summary>
    public class IconButton : Button
    {
        public static readonly DependencyProperty NormalIconProperty =
            DependencyProperty.Register(
                nameof(NormalIcon),
                typeof(ImageSource),
                typeof(IconButton));

        public static readonly DependencyProperty MouseOverIconProperty =
            DependencyProperty.Register(
                nameof(MouseOverIcon),
                typeof(ImageSource),
                typeof(IconButton));

        public static readonly DependencyProperty MouseDownIconProperty =
            DependencyProperty.Register(
                nameof(MouseDownIcon),
                typeof(ImageSource),
                typeof(IconButton));

        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.Register(
                nameof(MouseOverBackground),
                typeof(Brush),
                typeof(IconButton),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(224, 224, 224))));

        public static readonly DependencyProperty MouseOverForegroudProperty =
            DependencyProperty.Register(
                nameof(MouseOverForeground),
                typeof(Brush),
                typeof(IconButton),
                new PropertyMetadata(Brushes.Blue));

        /// <summary>
        /// The icon to show in the normal state.
        /// </summary>
        public ImageSource NormalIcon
        {
            get { return (ImageSource)GetValue(NormalIconProperty); }
            set { SetValue(NormalIconProperty, value); }
        }

        /// <summary>
        /// The icon to show in the mouse over state.
        /// </summary>
        public ImageSource MouseOverIcon
        {
            get { return (ImageSource)GetValue(MouseOverIconProperty); }
            set { SetValue(MouseOverIconProperty, value); }
        }

        /// <summary>
        /// The image to show when mouse is pressed but not released.
        /// </summary>
        public ImageSource MouseDownIcon
        {
            get { return (ImageSource)GetValue(MouseDownIconProperty); }
            set { SetValue(MouseDownIconProperty, value); }
        }

        /// <summary>
        /// The brush of background in the mouse over state.
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return (Brush)GetValue(MouseOverBackgroundProperty); }
            set { SetValue(MouseOverBackgroundProperty, value); }
        }

        /// <summary>
        /// The brush of foreground in the mouse over state.
        /// </summary>
        public Brush MouseOverForeground
        {
            get { return (Brush)GetValue(MouseOverForegroudProperty); }
            set { SetValue(MouseOverForegroudProperty, value); }
        }
    }
}
