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
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Custom control ImageToggleButton.
    /// </summary>
    public class ImageToggleButton : ToggleButton
    {
        public static readonly DependencyProperty CheckedImageProperty =
            DependencyProperty.Register(
                nameof(CheckedImage),
                typeof(ImageSource),
                typeof(ImageToggleButton));

        public static readonly DependencyProperty UncheckedImageProperty =
            DependencyProperty.Register(
                nameof(UncheckedImage),
                typeof(ImageSource),
                typeof(ImageToggleButton));

        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.Register(
                nameof(MouseOverBackground),
                typeof(Brush),
                typeof(ImageToggleButton),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(224, 224, 224))));

        public static readonly DependencyProperty MouseOverForegroudProperty =
            DependencyProperty.Register(
                nameof(MouseOverForeground),
                typeof(Brush),
                typeof(ImageToggleButton),
                new PropertyMetadata(Brushes.Blue));

        /// <summary>
        /// The image to show in the checked state.
        /// </summary>
        public ImageSource CheckedImage
        {
            get { return (ImageSource)GetValue(CheckedImageProperty); }
            set { SetValue(CheckedImageProperty, value); }
        }

        /// <summary>
        /// The image to show in the unchecked state.
        /// </summary>  
        public ImageSource UncheckedImage
        {
            get { return (ImageSource)GetValue(UncheckedImageProperty); }
            set { SetValue(UncheckedImageProperty, value); }
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
