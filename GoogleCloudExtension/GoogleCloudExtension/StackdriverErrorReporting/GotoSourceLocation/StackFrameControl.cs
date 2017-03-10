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
    /// Custom control that displays stack frame message with an optional source file link.
    /// </summary>
    public class StackFrameControl : Control
    {
        public static readonly DependencyProperty FrameProperty =
            DependencyProperty.Register(
                nameof(Frame),
                typeof(StackFrame),
                typeof(StackFrameControl),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the stack frame object as data source.
        /// <seealso cref="FrameProperty"/>.
        /// </summary>
        public StackFrame Frame
        {
            get { return (StackFrame)GetValue(FrameProperty); }
            set { SetValue(FrameProperty, value); }
        }
    }
}
