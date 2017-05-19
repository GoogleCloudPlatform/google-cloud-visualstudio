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

        public SolutionUserOptions(object settingObjcect)
        {
            _settingObject = settingObjcect;
            GetSettingProperties();
        }

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
        /// <param name="key"></param>
        /// <param name="value"></param>
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
            // This queries string type instance properties (not static etc) with both public Get, Set methods.
            var props = _settingObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => Attribute.IsDefined(prop, typeof(SolutionSettingKeyAttribute)) &&
                    prop.CanRead && prop.CanWrite &&
                    prop.GetGetMethod(nonPublic: false) != null &&
                    prop.GetSetMethod(nonPublic: false) != null &&
                    prop.PropertyType == typeof(string));

            foreach (PropertyInfo prop in props)
            {
                var optionAttribute = prop.GetCustomAttribute<SolutionSettingKeyAttribute>();
                _properties.Add(optionAttribute.KeyName, prop);
            }
        }
    }
}
