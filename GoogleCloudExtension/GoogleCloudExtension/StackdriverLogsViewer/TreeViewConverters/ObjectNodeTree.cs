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
using GoogleCloudExtension.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Log Viewer detail tree view object node.
    /// An object node contains the object name, optional object.ToString() as value,
    /// and optional object properties as Children.
    /// 
    /// The object properties are of ObjectNodeTree type too,
    /// thus the ObjectNodeTree forms a tree structure.
    /// </summary>
    public class ObjectNodeTree
    {
        protected string _filterLabelOverride;
        protected string _fitlerValueOverride;

        /// <summary>
        /// The list of supported classes.
        /// </summary>
        private readonly static Type[] s_supportedTypes = new Type[]
        {
            typeof(MonitoredResource),
            typeof(HttpRequest),
            typeof(LogEntryOperation),
            typeof(LogEntrySourceLocation),
            typeof(LogLine)
        };

        /// <summary>
        /// For lazy creation of children object.
        /// </summary>
        private Lazy<List<ObjectNodeTree>> _children = new Lazy<List<ObjectNodeTree>>();

        /// <summary>
        /// The display name of the object.
        /// </summary>
        private string _name;

        /// <summary>
        /// Gets the DisplayValue
        /// Tree node displays label in format of Name : DisplayValue.  
        /// </summary>
        public string NodeValue { get; private set; }

        /// <summary>
        /// Gets the object name with optional colon.
        /// </summary>
        public string Name => String.IsNullOrWhiteSpace(NodeValue) ? _name :
            String.Format(Resources.LogViewerDetailTreeViewNameLabelFormat, _name);

        /// <summary>
        /// Gets the label name for showing maching logs filter.
        /// </summary>
        public string FilterLabel => _filterLabelOverride ?? _name;

        /// <summary>
        /// Gets the value for showing maching logs filter.
        /// </summary>
        public string FilterValue => GetFilterValue();

        /// <summary>
        /// Gets tree node children.
        /// It contains all properties of the node object.
        /// </summary>
        public List<ObjectNodeTree> Children => _children.Value;

        /// <summary>
        /// The parent <seealso cref="ObjectNodeTree"/> object.
        /// </summary>
        public ObjectNodeTree Parent { get; }

        /// <summary>
        /// Create an instance of the <seealso cref="ObjectNodeTree"/> class.
        /// </summary>
        /// <param name="name">object name</param>
        /// <param name="obj">An object</param>
        /// <param name="parent">The parent <seealso cref="ObjectNodeTree"/> object.</param>
        public ObjectNodeTree(string name, object obj, ObjectNodeTree parent)
        {
            Parent = parent;
            _name = name;
            if (obj == null)
            {
                return;
            }

            ParseObjectTree(obj);
        }

        /// <summary>
        /// Parsing the object properties recursively as a tree.
        /// </summary>
        protected virtual void ParseObjectTree(object obj)
        {
            Type type = obj.GetType();

            if (obj.IsNumericType() || obj is string || obj is Boolean)
            {
                NodeValue = obj.ToString();
            }
            else if (obj is DateTime)
            {
                NodeValue = ((DateTime)obj).ToString(Resources.LogViewerLogItemDateTimeFormat);
                _fitlerValueOverride = ((DateTime)obj).ToUniversalTime().ToString("O");
            }
            else if (type.IsArray)
            {
                Debug.WriteLine($"Json object, {_name},  {type.Name}");
                ParseArray(obj as IEnumerable);
            }
            else if (obj is IDictionary)
            {
                ParseDictionary(obj as IDictionary);
            }
            else if (s_supportedTypes.Contains(type))
            {
                ParseClassProperties(obj, type);
            }
            else
            {
                Debug.Assert(false, $"Unexpected type found, ${type}");
            }
        }

        protected ObjectNodeTree AddChildren(string name, object obj)
        {
            ObjectNodeTree newNode = null;
            if (obj != null)
            {
                if (obj is JObject)
                {
                    newNode = new JObjectNode(name, obj as JObject, this);
                }
                else if (obj is JArray)
                {
                    newNode = new JArrayNode(name, obj as JArray, this);
                }
                else
                {
                    newNode = new ObjectNodeTree(name, obj, this);
                }
            }

            if (newNode != null)
            {
                Children.Add(newNode);
            }
            return newNode;
        }

        #region parser
        protected void ParseArray(IEnumerable enumerable)
        {
            int i = 0;
            foreach (var element in enumerable)
            {
                var node = AddChildren(String.Format(Resources.LogViewerDetailTreeViewArrayIndexFormat, i), element);
                node._filterLabelOverride = "";
                ++i;
            }
        }

        private void ParseDictionary(IDictionary dictionaryObject)
        {
            foreach (var key in dictionaryObject.Keys)
            {
                string name = key.ToString();
                AddChildren(name, dictionaryObject[key]);
                if (name.Contains('.'))
                {
                    Children.Last()._filterLabelOverride = $"\"{name}\"";
                }
            }
        }

        private static bool IsPropertyInfoGetValueException(Exception ex)
        {
            return ex is ArgumentException || ex is TargetException || ex is TargetParameterCountException
                   || ex is MethodAccessException || ex is TargetInvocationException;
        }

        private void ParseClassProperties(object obj, Type type)
        {
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
                        AddChildren(p.Name, v);
                    }
                }
                catch (Exception ex) when (IsPropertyInfoGetValueException(ex))
                {
                    // Value convertion error, display a general error so as not to hide the problem.
                    AddChildren(p.Name, Resources.LogViewerDataConversionGenericError);
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
        #endregion

        private string GetFilterValue()
        {
            var value = _fitlerValueOverride ?? NodeValue;
            return StringUtils.Escape(value);
        }
    }
}
