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

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// This control represents a label with a help anchor to show help messages to the user.
    /// </summary>
    public class LabelWithHelp : System.Windows.Controls.Label
    {
        public static readonly DependencyProperty HelpContentProperty =
            DependencyProperty.Register(
                nameof(HelpContent),
                typeof(object),
                typeof(LabelWithHelp));

        /// <summary>
        /// This property contains the help content to be shown when hovering over the help anchor.
        /// </summary>
        public object HelpContent
        {
            get { return GetValue(HelpContentProperty); }
            set { SetValue(HelpContentProperty, value); }
        }
    }
}
