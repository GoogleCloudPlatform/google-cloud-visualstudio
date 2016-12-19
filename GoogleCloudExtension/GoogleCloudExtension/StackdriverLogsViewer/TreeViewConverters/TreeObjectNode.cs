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

using Google.Apis.Logging.v2.Data;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    /// <summary>
    /// Log Viewer detail tree view object node.
    /// An object node contains the object name, optional object.ToString() as value,
    /// and optional object properties as Children.
    /// 
    /// The object properties are of ObjectNodeTree type too,
    /// thus the ObjectNodeTree forms a tree structure.
    /// </summary>
    internal class ObjectNodeTree
    {
        /// <summary>
        /// The list of supported classes.
        /// </summary>
        private readonly static Type[] s_supportedTypes = new Type[]
        {
            typeof(MonitoredResource),
            typeof(HttpRequest),
            typeof(LogEntryOperation),
            typeof(SourceLocation),
            typeof(LogLine),
            typeof(LogEntry)
        };

        /// <summary>
        /// Gets the DisplayValue
        /// Tree node displays label in format of Name : DisplayValue.  
        /// </summary>
        public string NodeValue { get; private set; }

        /// <summary>
        /// Gets the obj visibility. 
        /// Do not display ":" if the NodeValue is empty
        /// </summary>
        public Visibility ValueVisibility => 
            String.IsNullOrWhiteSpace(NodeValue) ? Visibility.Hidden : Visibility.Visible;

        /// <summary>
        /// Gets the object name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets tree node children.
        /// It contains all properties of the node object.
        /// </summary>
        public object Children { get; private set; }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="obj">An object</param>
        public ObjectNodeTree(object obj): this("", obj)
        { }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="name">object name</param>
        /// <param name="obj">An object</param>
        private ObjectNodeTree(string name, object obj)
        {
            Name = name;
            if (obj == null)
            {
                NodeValue = null;
                return;
            }

            ParseObjectTree(obj);
        }

        /// <summary>
        /// Parsing the object properties recursively as a tree.
        /// </summary>
        private void ParseObjectTree(object obj)
        {
            Type type = obj.GetType();

            if (obj.IsNumericType() || obj is string || obj is DateTime)
            {
                NodeValue = obj.ToString();
            }
            else if (type.IsArray)
            {
                ParseEnumerable(obj);
            }
            // There is no easy way to parse generic IDictionarytype into ObjectNodeTree.
            // Display them as Payload object.
            else if (obj.IsDictionaryObject())
            {
                ParseDictionary(obj);
            }
            else if (s_supportedTypes.Contains(type))
            {
                ParseClassProperties(obj, type);
            }
            else
            {
                // The best effort.
                NodeValue = obj.ToString();
            }
        }

        #region parser
        private ObservableCollection<object> CreateCollectionChildren()
        {
            var collection = new ObservableCollection<object>();
            Children = collection;
            return collection;
        }

        private void ParseEnumerable(object obj)
        {
            var collection = CreateCollectionChildren();
            IEnumerable arr = obj as IEnumerable;
            Debug.Assert(arr != null);
            if (arr == null)
            {
                // Don't expect null. Be protective
                NodeValue = obj.ToString();
                return;
            }

            int i = 0;
            foreach (var ele in arr)
            {
                collection.Add(new ObjectNodeTree($"[{i}]", ele));
                ++i;
            }

            NodeValue = $"[{i}]";
        }

        private void ParseDictionary(object obj)
        {
            var collection = CreateCollectionChildren();
            var dict = obj as IDictionary;
            Debug.Assert(dict != null);
            if (dict == null)
            {
                // Don't expect null. Be protective
                NodeValue = obj.ToString();
                return;
            }

            foreach (var key in dict.Keys)
            {
                string name = key.ToString();
                collection.Add(new ObjectNodeTree(name, dict[key]));
            }
        }

        private void ParseClassProperties(object obj, Type type)
        {
            var collection = CreateCollectionChildren();
            PropertyInfo[] properties = type.GetProperties();
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
                        collection.Add(new ObjectNodeTree(p.Name, v));
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException || ex is TargetException || ex is TargetParameterCountException
                        || ex is MethodAccessException || ex is TargetInvocationException)
                    {
                        // Value convertion error, display for awarness.
                        collection.Add(
                            new ObjectNodeTree(p.Name, $"Type: {p.PropertyType}. PropertyInfo.GetValue failed."));
                        Debug.WriteLine(ex.ToString());
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
        #endregion
    }
}
