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
    public class ImageButton : Button
    {
        public static readonly DependencyProperty NormalImageProperty =
            DependencyProperty.Register(
                nameof(NormalImage),
                typeof(ImageSource),
                typeof(ImageButton));

        public static readonly DependencyProperty MouseOverImageProperty =
            DependencyProperty.Register(
                nameof(MouseOverImage),
                typeof(ImageSource),
                typeof(ImageButton));

        public static readonly DependencyProperty MouseDownImageProperty =
            DependencyProperty.Register(
                nameof(MouseDownImage),
                typeof(ImageSource),
                typeof(ImageButton));

        /// <summary>
        /// The image to show in the normal state.
        /// </summary>
        public ImageSource NormalImage
        {
            get { return (ImageSource)GetValue(NormalImageProperty); }
            set { SetValue(NormalImageProperty, value); }
        }

        /// <summary>
        /// The image to show in the mouse over state.
        /// </summary>
        public ImageSource MouseOverImage
        {
            get { return (ImageSource)GetValue(MouseOverImageProperty); }
            set { SetValue(MouseOverImageProperty, value); }
        }

        /// <summary>
        /// The image to show when mouse is pressed but not released.
        /// </summary>
        public ImageSource MouseDownImage
        {
            get { return (ImageSource)GetValue(MouseDownImageProperty); }
            set { SetValue(MouseDownImageProperty, value); }
        }
    }
}
