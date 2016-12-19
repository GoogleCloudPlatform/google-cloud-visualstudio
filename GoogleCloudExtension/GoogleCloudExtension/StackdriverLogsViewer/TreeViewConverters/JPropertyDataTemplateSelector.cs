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
using System.Diagnostics;

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    public sealed class JPropertyDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PrimitivePropertyTemplate { get; set; }
        public DataTemplate ComplexPropertyTemplate { get; set; }
        public DataTemplate ArrayPropertyTemplate { get; set; }
        public DataTemplate ObjectPropertyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if(item == null)
                return null;

            var frameworkElement = container as FrameworkElement;
            if(frameworkElement == null)
                return null;

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

            if (type.Name == "KeyValuePair`2")
            {

                try
                {
                    object value = ((dynamic)item).Value;
                    // var keyValuePair = (System.Collections.Generic.KeyValuePair<string, object>)item;
                    if (value.GetType().Namespace.StartsWith("Newtonsoft.Json"))
                    {
                        return frameworkElement.FindResource("KeyValuePairJObject") as DataTemplate;
                    }
                    else if (value is string || value.GetType().IsValueType)
                    {
                        return frameworkElement.FindResource("KeyValuePair") as DataTemplate;
                    }
                    else
                    {
                        Debug.WriteLine($"type.Name value.GetType()");
                    }
                }
                catch
                {
                    Debug.WriteLine("Exception at converting keyValuePair");

                }

            }

            var key = new DataTemplateKey(type);
            try
            {
                return frameworkElement.FindResource(key) as DataTemplate;
            }
            catch
            {
                return null;
            }
        }
    }
}
