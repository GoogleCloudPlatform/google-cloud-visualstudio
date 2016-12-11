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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    internal class TreeObjectNode
    {
        #region Private Properties

        private string _name;
        private object _displayValue;
        private Type _type;

        #endregion

        #region Constructor

        public TreeObjectNode(object value)
        {
            ParseObjectTree("root", value, value.GetType());
        }

        public TreeObjectNode(string name, object value)
        {
            ParseObjectTree(name, value, value.GetType());
        }

        public TreeObjectNode(object value, Type t)
        {
            ParseObjectTree("root", value, t);
        }

        public TreeObjectNode(string name, object value, Type t)
        {
            ParseObjectTree(name, value, t);
        }

        #endregion

        #region Public Properties

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public Visibility ValueVisibility
        {
            get
            {
                if (DisplayValue == null || string.IsNullOrWhiteSpace(DisplayValue.ToString()))
                {
                    return Visibility.Hidden;
                }

                return Visibility.Visible;
            }
        }

        public object DisplayValue
        {
            get
            {
                return _displayValue;
            }
        }

        public Type Type
        {
            get
            {
                return _type;
            }
        }

        public ObservableCollection<object> Children { get; set; }

        #endregion

        #region Private Methods

        private bool IsList(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(IList<>));
        }

        private bool IsDictionary(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(IDictionary<,>));
        }

        private bool IsListObject(object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(IList<>));
        }

        private bool IsDictionaryObject(object o)
        {
            if (o == null) return false;
            return o is IDictionary &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(IDictionary<,>));
        }

        private void ParseObjectTree(string name, object value, Type type)
        {
            ParseObjectTreeImpl(name, value, type);
            if (Children?.Count > 0)
            {
                _displayValue = string.Empty;
            }
            else
            {
                if (_displayValue == null && value != null)
                {
                    _displayValue = value.ToString();
                }
            }
        }

        private void ParseObjectTreeImpl(string name, object value, Type type)
        {
            Children = new ObservableCollection<object>();

            _type = type;
            _name = name;

            if (value == null)
            {
                return;
            }

            if (IsDictionaryObject(value) || IsListObject(value))
            {
                // _value = type.Name;
                Children.Add(new Payload(name, value));
                return;
            }

            if (value != null)
            {
                if (value is string && type != typeof(object))
                {
                    if (value != null)
                    {
                        _displayValue = "\"" + value + "\"";
                    }
                }
                else if (value is double || value is bool || value is int || value is float || value is long || value is decimal)
                {
                    _displayValue = value;
                }
                else
                {
                    _displayValue = "{" + value.ToString() + "}";
                }
            }

            // Add some well known types that need to be excluded here.
            if (type == typeof(string))
            {
                return;
            }

            PropertyInfo[] props = type.GetProperties();

            if (props.Length == 0 && type.IsClass && value is IEnumerable && !(value is string))
            {
                IEnumerable arr = value as IEnumerable;

                if (arr != null)
                {
                    int i = 0;
                    foreach (object element in arr)
                    {
                        Children.Add(new TreeObjectNode("[" + i + "]", element, element.GetType()));
                        i++;
                    }

                }
            }

            foreach (PropertyInfo p in props)
            {
                if (!p.PropertyType.IsPublic)
                {
                    continue;
                }

                if (IsDictionary(p.PropertyType) || IsList(p.PropertyType))
                {
                    object v = p.GetValue(value, null);
                    if (v != null)
                    {
                        Children.Add(new Payload(p.Name, v));
                    }
                }
                else if (p.PropertyType.IsClass || p.PropertyType.IsArray)
                {
                    if (p.PropertyType.IsArray)
                    {
                        try
                        {
                            object v = p.GetValue(value);
                            IEnumerable arr = v as IEnumerable;

                            TreeObjectNode arrayNode = new TreeObjectNode(p.Name, arr.ToString(), typeof(object));

                            if (arr != null)
                            {
                                int i = 0, k = 0;
                                TreeObjectNode arrayNode2;

                                foreach (object element in arr)
                                {
                                    //Handle 2D arrays
                                    if (element is IEnumerable && !(element is string))
                                    {
                                        arrayNode2 = new TreeObjectNode("[" + i + "]", element.ToString(), typeof(object));

                                        IEnumerable arr2 = element as IEnumerable;
                                        k = 0;

                                        foreach (object e in arr2)
                                        {
                                            arrayNode2.Children.Add(new TreeObjectNode("[" + k + "]", e, e.GetType()));
                                            k++;
                                        }

                                        arrayNode.Children.Add(arrayNode2);
                                    }
                                    else
                                    {
                                        arrayNode.Children.Add(new TreeObjectNode("[" + i + "]", element, element.GetType()));
                                    }
                                    i++;
                                }

                            }

                            Children.Add(arrayNode);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        try
                        {
                            // String value is added here
                            object v = p.GetValue(value, null);
                            if (v != null)
                            {
                                Children.Add(new TreeObjectNode(p.Name, v, p.PropertyType));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                }
                else if (p.PropertyType.IsValueType && !(value is string))
                {
                    try
                    {
                        object v = p.GetValue(value);

                        if (v != null)
                        {
                            Children.Add(new TreeObjectNode(p.Name, v, p.PropertyType));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    try
                    {
                        object v = p.GetValue(value);
                        if (v != null)
                        {
                            Children.Add(new TreeObjectNode(p.Name, v, p.PropertyType));
                        }
                    }
                    catch (Exception ex)
                    {
                        Children.Add(new Payload(p.Name, $"Type: {p.PropertyType}. PropertyInfo.GetValue failed"));
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }
        }

        #endregion
    }
}
