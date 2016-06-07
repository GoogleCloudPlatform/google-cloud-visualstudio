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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Converts a boolean value into a Visibility enum value. True will mean Visible and False Collapsed.
    /// Note: Only Convert is implemented, so this is not a bidirectional converter, do not use on TwoWay bindings.
    /// </summary>
    public class VisibilityConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Determine if the value to convert should be negated before the conversion
        /// takes place, that is, if <c>IsNegated</c> is <c>True</c> then when converting
        /// <c>False</c> will be visible and <c>True</c> will be collapsed.
        /// </summary>
        public bool IsNegated { get; set; }

        /// <summary>
        /// Whether logging the value being converted is enabled.
        /// </summary>
        public bool LoggingEnabled { get; set; }

        /// <summary>
        /// The prefix string to use for the log messages, useful for finding the entries in the 
        /// Output window.
        /// </summary>
        public string LoggingPrefix { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool toConvert = IsNegated ^ (bool)value;
                var result = toConvert ? Visibility.Visible : Visibility.Collapsed;
                if (LoggingEnabled)
                {
                    Debug.WriteLine($"{nameof(VisibilityConverter)}: {LoggingPrefix} converting {value} to {result}");
                }
                return result;
            }
            Debug.WriteLine($"Value should be a boolean: {value}");
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
