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
using System.Globalization;
using System.Windows.Data;
using Newtonsoft.Json.Linq;

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    /// <summary>
    /// Convert JValue to tree view display string.
    /// </summary>
    public sealed class JValueObjectConverter : IValueConverter
    {
        /// <summary>
        /// Impletement the Convert method of IValueConverter.
        /// Converts a JValue.
        /// </summary>
        /// <returns>A Jason value as string.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var jVal = value as JValue;
            if (jVal != null)
            {
                switch (jVal.Type)
                {
                    case JTokenType.String:
                        return $"\"{jVal.Value}\"";

                    case JTokenType.Null:
                        return "Null";
                }
            }

            return value;
        }

        /// <summary>
        /// Placeholder of of ConvertBack method of IValueConverter. 
        /// Not needed and hence not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">Throw for the ConvertBack is not supported.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(GetType().Name + " can only be used for one way conversion.");
        }
    }
}
