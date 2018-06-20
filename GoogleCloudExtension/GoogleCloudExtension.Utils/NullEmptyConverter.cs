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
using System.Windows.Data;
using System.Windows.Markup;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Checks if the string to convert is null or empty, and returns one of two values.
    /// </summary>
    /// <typeparam name="T">The types of the values to output.</typeparam>
    public abstract class NullEmptyConverter<T> : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// If true, null, empty and whitespace values will return <see cref="NotEmptyValue"/>
        /// instead of <see cref="EmptyValue"/>.
        /// </summary>
        public bool Invert { get; set; }

        protected abstract T NotEmptyValue { get; }

        protected abstract T EmptyValue { get; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue;
            if (value is IConvertible convertible)
            {
                stringValue = convertible.ToString(culture);
            }
            else
            {
                stringValue = value?.ToString();
            }

            bool isEmpty = string.IsNullOrWhiteSpace(stringValue);
            if (Invert)
            {
                isEmpty = !isEmpty;
            }
            return isEmpty ? EmptyValue : NotEmptyValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        /// <summary>
        /// Implement interface MarkupExtension.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}