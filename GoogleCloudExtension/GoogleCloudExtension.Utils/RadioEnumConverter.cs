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
    /// This converter can be used to map multiple radio buttons to a single enum property.
    /// </summary>
    public class RadioEnumConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// The enum value represented by this radio button.
        /// </summary>
        public object Target { private get; set; }

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        /// <summary>
        /// Returns true if the input value is the target value, false otherwise.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, Target);
        }

        /// <summary>
        /// Returns <see cref="Target"/> if the input value is true, <see cref="Binding.DoNothing"/> otherwise.
        /// </summary>
        /// <remarks>
        /// Returning <see cref="Binding.DoNothing"/> prevents this radio button from interfearing with the other
        /// radio buttons setting the value when they are selected.
        /// </remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, true) ? Target : Binding.DoNothing;
        }
    }
}
