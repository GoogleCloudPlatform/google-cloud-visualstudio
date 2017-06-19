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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// If the object is null, or if it is string type and is empty or whitespace,
    /// set the visibility to <seealso cref="Visibility.Collapsed"/>.
    /// Otherwise, set the visibility as <seealso cref="Visibility.Visible"/>.
    /// Note: Only Convert is implemented, so this is not a bidirectional converter, do not use on TwoWay bindings.
    /// </summary>
    public class NullEmptyInvisibleConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// If true, null, empty and whitespace values will return <see cref="Visibility.Visible"/>
        /// instead of <see cref="Visibility.Collapsed"/>.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;
            if (stringValue != null)
            {
                return (String.IsNullOrWhiteSpace(stringValue) ^ Invert) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if ((value == null) ^ Invert)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implement interface MarkupExtension.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}

