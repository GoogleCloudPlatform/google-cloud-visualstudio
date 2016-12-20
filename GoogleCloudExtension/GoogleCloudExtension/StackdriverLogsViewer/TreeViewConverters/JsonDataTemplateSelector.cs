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
using Newtonsoft.Json.Linq;

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    /// <summary>
    /// DataTemplate selector for Json converters.
    /// </summary>
    public sealed class JsonDataTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets Primitive Property DataTemplate.
        /// </summary>
        public DataTemplate PrimitivePropertyTemplate { get; set; }

        /// <summary>
        /// Gets or sets Complex Property DataTemplate.
        /// </summary>
        public DataTemplate ComplexPropertyTemplate { get; set; }

        /// <summary>
        /// Gets or sets Array Property DataTemplate.
        /// </summary>
        public DataTemplate ArrayPropertyTemplate { get; set; }

        /// <summary>
        /// Gets or sets Object Property DataTemplate.
        /// </summary>
        public DataTemplate ObjectPropertyTemplate { get; set; }

        /// <summary>
        /// Help WPF view to select a DataTemplate for the given Json object.
        /// </summary>
        /// <param name="item">A Json object</param>
        /// <param name="container">The container of the DataTemplate </param>
        /// <returns>The selected <seealso cref="DataTemplate"/></returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                return null;
            }

            var frameworkElement = container as FrameworkElement;
            if (frameworkElement == null)
            {
                return null;
            }

            var type = item.GetType();
            if (type == typeof(JProperty))
            {
                var jProperty = item as JProperty;
                switch (jProperty.Value.Type)
                {
                    case JTokenType.Object:
                        return frameworkElement.FindResource("ObjectPropertyTemplate") as DataTemplate;
                    case JTokenType.Array:
                        return frameworkElement.FindResource("ArrayPropertyTemplate") as DataTemplate;
                    default:
                        return frameworkElement.FindResource("PrimitivePropertyTemplate") as DataTemplate;
                }
            }

            var key = new DataTemplateKey(type);
            return frameworkElement.FindResource(key) as DataTemplate;
        }
    }
}
