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

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    /// <summary>
    /// Convert LogItem object to a collection of <seealso cref="ObjectNodeTree"/> that treeview can display.
    /// </summary>
    internal sealed class LogItemConverter : IValueConverter
    {
        /// <summary>
        /// Impletement the Convert method of IValueConverter.
        /// Converts a value.
        /// </summary>
        /// <param name="value">
        /// The value produced by the binding source.
        /// Here, a LogItem object is expected.
        /// </param>
        /// <param name="targetType">Ignored. The type of the binding target property.</param>
        /// <param name="parameter">Ignored. The converter parameter to use. .</param>
        /// <param name="culture">Ignored. The culture to use in the converter.</param>
        /// <returns>A collection of ObjectNodeTree objects.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is LogItem))
            {
                throw new NotSupportedException("GetType().Name can only converte value type of LogItem");
            }

            LogItem log = value as LogItem;
            return new ObjectNodeTree(log.Entry).Children;
        }

        /// <summary>
        /// Placeholder of of ConvertBack method of IValueConverter. 
        /// Not needed and hence not supported.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(GetType().Name + " can only be used for one way conversion.");
        }
    }
}
