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
    /// <summary>
    /// Utility methods primarily used by ObjectNodeTree 
    /// </summary>
    public static class TypeUtil
    {
        /// <summary>
        /// Check if the type is a IList generic type.
        /// </summary>
        public static bool IsListType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(IList<>));
        }

        /// <summary>
        /// Check if the type is IDictionary type.
        /// </summary>
        public static bool IsDictionaryType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(IDictionary<,>));
        }

        /// <summary>
        /// Check if the object is a IList type
        /// </summary>
        public static bool IsListObject(this object obj)
        {
            return obj != null && obj is IList && obj.GetType().IsListType();
        }

        /// <summary>
        /// Check if the object is IDictionary
        /// </summary>
        public static bool IsDictionaryObject(this object obj)
        {
            return obj != null && obj.GetType().IsDictionaryType();
        }

        /// <summary>
        /// Check if the object is Numeric type
        /// </summary>
        public static bool IsNumericType(this object obj)
        {
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Log Viewer detail tree view object node.
    /// An object node contains the object name, obj and object properties as children.
    /// The object properties are of ObjectNodeTree type or Payload type. 
    /// With the children, the ObjectNodeTree itself forms a tree structure.
    /// </summary>
    internal class ObjectNodeTree
    {
        private Type _type;

        /// <summary>
        /// Gets the DisplayValue
        /// Tree node displays label in format of Name : DisplayValue.  
        /// </summary>
        public object NodeValue { get; private set; }

        /// <summary>
        /// Gets the obj visibility. 
        /// This is to control whether ":" is displayed.
        /// </summary>
        public Visibility ValueVisibility => 
            String.IsNullOrWhiteSpace(NodeValue?.ToString()) ? Visibility.Hidden : Visibility.Visible;

        /// <summary>
        /// Gets the object name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets tree node children.
        /// It contains all properties of the node object.
        /// </summary>
        public ObservableCollection<object> Children { get; private set; }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="obj">An object</param>
        public ObjectNodeTree(object obj)
        {
            ParseObjectTree("root", obj, obj.GetType());
        }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="name">object name</param>
        /// <param name="obj">An object</param>
        public ObjectNodeTree(string name, object obj)
        {
            ParseObjectTree(name, obj, obj.GetType());
        }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="obj">An object</param>
        /// <param name="type">object type.</param>
        public ObjectNodeTree(object obj, Type type)
        {
            ParseObjectTree("root", obj, type);
        }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="name">object name</param>
        /// <param name="obj">An object</param>
        /// <param name="type">object type.</param>
        public ObjectNodeTree(string name, object obj, Type type)
        {
            ParseObjectTree(name, obj, type);
        }

        /// <summary>
        /// Parse the object properties recursively.
        /// </summary>
        private void ParseObjectTree(string name, object obj, Type type)
        {
            ParseObjectTreeImpl(name, obj, type);
            if (Children?.Count > 0)
            {
                NodeValue = null;
            }
            else
            {
                if (NodeValue == null && obj != null)
                {
                    NodeValue = obj.ToString();
                }
            }
        }

        /// <summary>
        /// The major logic of parsing the object properties as a tree.
        /// </summary>
        /// <param name="name">object name label.</param>
        /// <param name="obj">object obj, the object itself.</param>
        /// <param name="type">object type.</param>
        private void ParseObjectTreeImpl(string name, object obj, Type type)
        {
            _type = type;
            Name = name;
            Children = new ObservableCollection<object>();

            if (obj == null)
            {
                return;
            }

            //if (type != obj.GetType())
            //{
            //    Debug.Assert(false);
            //}

            // There is no easy way to parse generic Dictionary or List type into ObjectNodeTree.
            // Display them as Payload object.
            if (obj.IsDictionaryObject() || obj.IsListObject())
            {
                Children.Add(new Payload(name, obj));
                return;
            }

            if (obj is string)
            {
                NodeValue = $"\"{obj}\"";
                return;
            }
            else
            if (obj.IsNumericType())
            {
                NodeValue = obj;
                return;
            }
            else
            {
                NodeValue = "{" + obj.ToString() + "}";
            }

            PropertyInfo[] properties = type.GetProperties();

            if (properties.Length == 0 && type.IsClass && obj is IEnumerable)
            {
                IEnumerable arr = obj as IEnumerable;
                if (arr != null)
                {
                    int i = 0;
                    foreach (object element in arr)
                    {
                        Children.Add(new ObjectNodeTree("[" + i + "]", element, element.GetType()));
                        ++i;
                    }
                }
            }

            foreach (PropertyInfo p in properties)
            {
                if (!p.PropertyType.IsPublic)
                {
                    continue;
                }

                if (p.PropertyType.IsDictionaryType() || p.PropertyType.IsListType())
                {
                    object v = p.GetValue(obj, null);
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
                            object v = p.GetValue(obj);
                            IEnumerable arr = v as IEnumerable;

                            ObjectNodeTree arrayNode = new ObjectNodeTree(p.Name, arr.ToString(), typeof(object));

                            if (arr != null)
                            {
                                int i = 0, k = 0;
                                ObjectNodeTree arrayNode2;

                                foreach (object element in arr)
                                {
                                    //Handle 2D arrays
                                    if (element is IEnumerable && !(element is string))
                                    {
                                        arrayNode2 = new ObjectNodeTree("[" + i + "]", element.ToString(), typeof(object));

                                        IEnumerable arr2 = element as IEnumerable;
                                        k = 0;

                                        foreach (object e in arr2)
                                        {
                                            arrayNode2.Children.Add(new ObjectNodeTree("[" + k + "]", e, e.GetType()));
                                            k++;
                                        }

                                        arrayNode.Children.Add(arrayNode2);
                                    }
                                    else
                                    {
                                        arrayNode.Children.Add(new ObjectNodeTree("[" + i + "]", element, element.GetType()));
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
                            object v = p.GetValue(obj, null);
                            if (v != null)
                            {
                                Children.Add(new ObjectNodeTree(p.Name, v, p.PropertyType));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                }
                else if (p.PropertyType.IsValueType && !(obj is string))
                {
                    try
                    {
                        object v = p.GetValue(obj);

                        if (v != null)
                        {
                            Children.Add(new ObjectNodeTree(p.Name, v, p.PropertyType));
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
                        object v = p.GetValue(obj);
                        if (v != null)
                        {
                            Children.Add(new ObjectNodeTree(p.Name, v, p.PropertyType));
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
    }
}
