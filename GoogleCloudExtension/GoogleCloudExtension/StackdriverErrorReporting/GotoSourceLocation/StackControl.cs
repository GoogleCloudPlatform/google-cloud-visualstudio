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

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// The control that displays all parsed stack frames.
    /// </summary>
    public class StackControl : Control
    {
        public static readonly DependencyProperty ParsedStacksProperty =
            DependencyProperty.Register(
                nameof(ParsedStacks),
                typeof(ParsedException),
                typeof(StackControl),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <seealso cref="ParsedException"/> as data source.
        /// <seealso cref="ParsedStacksProperty"/>.
        /// </summary>
        public ParsedException ParsedStacks
        {
            get { return (ParsedException)GetValue(ParsedStacksProperty); }
            set { SetValue(ParsedStacksProperty, value); }
        }
    }
}
