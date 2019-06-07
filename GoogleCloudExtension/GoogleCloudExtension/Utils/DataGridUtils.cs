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
using System.Windows.Media;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers for DataGrid control code behind.
    /// </summary>
    internal static class DataGridUtils
    {
        /// <summary>
        /// Get the first ancestor control element of type TUIElement.
        /// </summary>
        /// <typeparam name="TUIElement">A <seealso cref="UIElement"/> type.</typeparam>
        /// <param name="obj">A <seealso cref="DependencyObject"/> element. </param>
        /// <returns>The object of TUIElement type.</returns>
        public static TUIElement FindAncestorControl<TUIElement>(DependencyObject obj) where TUIElement : UIElement
        {
            while ((obj != null) && !(obj is TUIElement))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as TUIElement;  // Note, "null as TUIElement" is valid and returns null.
        }
    }
}
