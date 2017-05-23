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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class queries properties with <seealso cref="SolutionSettingKeyAttribute"/>.
    /// And then it gets/sets values for the properties.
    /// </summary>
    public class SolutionUserOptions
    {
        private object _settingObject;
        private Dictionary<string, PropertyInfo> _properties = new Dictionary<string, PropertyInfo>();

        /// <summary>
        /// Initializes an instance of <seealso cref="SolutionUserOptions"/> object.
        /// </summary>
        /// <param name="settingObjcect">
        /// An object that contains public instance properties marked by <seealso cref="SolutionSettingKeyAttribute"/>.
        /// </param>
        public SolutionUserOptions(object settingObjcect)
        {
            _settingObject = settingObjcect;
            GetSettingProperties();
        }

        /// <summary>
        /// Get all option keys.
        /// </summary>
        public IEnumerable<string> Keys => _properties.Keys;

        /// <summary>
        /// Check if the settings contain the key.
        /// </summary>
        public bool Contains(string key) => _properties.ContainsKey(key);

        /// <summary>
        /// Return the option value of the given key.
        /// This is called when Visual Studio solution saves the key into .suo file.
        /// </summary>
        /// <param name="key">The option key.</param>
        public string Read(string key)
        {
            PropertyInfo propertyInfo;
            if (_properties.TryGetValue(key, out propertyInfo))
            {
                return propertyInfo.GetValue(_settingObject) as string;
            }
            return null;
        }

        /// <summary>
        /// Set option value of the key.
        /// This is called when Visual Studio reads settings from .suo file.
        /// </summary>
        /// <param name="key">The option key.</param>
        /// <param name="value">The option value.</param>
        public void Set(string key, string value)
        {
            PropertyInfo propertyInfo;
            if (_properties.TryGetValue(key, out propertyInfo))
            {
                propertyInfo.SetValue(_settingObject, value);
            }
        }

        private void GetSettingProperties()
        {
            // This queries for instance properties (not static etc)
            // that has SolutionSettingKeyAttribute attached to it
            var props = _settingObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => Attribute.IsDefined(prop, typeof(SolutionSettingKeyAttribute)));

            foreach (PropertyInfo prop in props)
            {
                if (prop.GetGetMethod(nonPublic: false) == null ||
                    prop.GetSetMethod(nonPublic: false) == null ||
                    prop.PropertyType != typeof(string))
                {
                    // This exception will shown when developer test/debug the code. Won't be shown to end user.
                    throw new NotSupportedException(
                        $@"{prop.Name} is not correctly defined. 
Currently, only string type is supported for SolutionSettingKey. 
And you must define both public get and public set on it.");
                }

                var optionAttribute = prop.GetCustomAttribute<SolutionSettingKeyAttribute>();
                _properties.Add(optionAttribute.KeyName, prop);
            }
        }
    }
}
