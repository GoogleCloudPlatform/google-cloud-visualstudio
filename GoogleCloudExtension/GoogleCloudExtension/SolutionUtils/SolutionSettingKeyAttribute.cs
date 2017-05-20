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

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// Define attribute that indicates a property is intended to be saved into Visual Studio .suo file.
    /// Refer <seealso cref="SolutionUserOptions"/> class for how it is used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SolutionSettingKeyAttribute : Attribute
    {
        /// <summary>
        /// The Visual Studio solution option name.
        /// It must not contain . /
        /// It also has lenght limit.
        /// </summary>
        public string KeyName { get; }

        public SolutionSettingKeyAttribute(string keyName)
        {
            KeyName = keyName;
        }
    }
}
