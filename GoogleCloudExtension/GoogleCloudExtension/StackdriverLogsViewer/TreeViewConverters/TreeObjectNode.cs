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
            return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        /// <summary>
        /// Check if the type is IDictionary type.
        /// </summary>
        public static bool IsDictionaryType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
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
            ParseObjectTree("root", obj);
        }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="name">object name</param>
        /// <param name="obj">An object</param>
        public ObjectNodeTree(string name, object obj)
        {
            ParseObjectTree(name, obj);
        }

        #region parser

        private void TryParse(Action parser)
        {
            try
            {
                parser();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.Assert(false);
            }
        }

        private void ParseArrayEnumerable(object obj)
        {
            IEnumerable arr = obj as IEnumerable;
            int i = 0;
            foreach (var ele in arr)
            {
                Children.Add(new ObjectNodeTree("[" + i + "]", ele));
                ++i;
            }

            NodeValue = $"[{i}]";
        }
        #endregion

        /// <summary>
        /// Parse the object properties recursively.
        /// </summary>
        private void ParseObjectTree(string name, object obj)
        {
            Name = name;
            if (obj == null)
            {
                NodeValue = "null";
                return;
            }

            Children = new ObservableCollection<object>();
            ParseObjectTreeImpl(name, obj);

            // To be sure NodeValue
            if (Children?.Count > 0)
            {
                // NodeValue = null;
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
        /// Parsing the object properties as a tree.
        /// </summary>
        private void ParseObjectTreeImpl(string name, object obj)
        {
            Type type = obj.GetType();

            if (obj.IsNumericType() || obj is string || obj is DateTime)
            {
                NodeValue = obj.ToString();
                return;
            }
            else if (type.IsArray)
            {
                ParseArrayEnumerable(obj);
                return;
            }            
            // There is no easy way to parse generic Dictionary or List type into ObjectNodeTree.
            // Display them as Payload object.
            else if (obj.IsDictionaryObject() || obj.IsListObject())
            {
                Children.Add(new Payload(name, obj));
                return;
            }

            PropertyInfo[] properties = type.GetProperties();
            if (type.IsClass && obj is IEnumerable)
            {
                ParseArrayEnumerable(obj);
                return;
            }

            foreach (PropertyInfo p in properties)
            {
                if (!p.PropertyType.IsPublic)
                {
                    continue;
                }

                try
                {
                    object v = p.GetValue(obj);
                    if (v != null)
                    {
                        Children.Add(new ObjectNodeTree(p.Name, v));
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
