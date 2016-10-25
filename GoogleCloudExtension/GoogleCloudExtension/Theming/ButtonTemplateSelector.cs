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

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// This class implements the <seealso cref="DataTemplateSelector"/> interface to choose the right
    /// button style (with the right size) depending on the contents of the button.
    /// </summary>
    public class ButtonTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Choose between the Wide and Standard sizes for a button depending on the information on 
        /// the button fino.
        /// </summary>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var buttonInfo = (DialogButtonInfo)item;
            var element = (FrameworkElement)container;

            if (buttonInfo.Caption.Length > 6)
            {
                return (DataTemplate)element.FindResource("DialogButtonTemplateWide");
            }
            else
            {
                return (DataTemplate)element.FindResource("DialogButtonTemplateStandard");
            }
        }
    }
}
