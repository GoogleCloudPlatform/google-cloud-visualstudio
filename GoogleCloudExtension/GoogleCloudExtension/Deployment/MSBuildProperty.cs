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

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// Represents an MSBuild property command line argument.
    /// </summary>
    public class MSBuildProperty
    {
        /// <summary>
        /// The name of the MSBuild property to set.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The value to set the MSBuild property to.
        /// </summary>
        public string PropertyValue { get; }

        /// <summary>
        /// Creates an MSBuild property command line argument.
        /// </summary>
        /// <param name="propertyName">The name of the MSBuild property.</param>
        /// <param name="propertyValue">The value of the MSBuild property.</param>
        public MSBuildProperty(string propertyName, string propertyValue)
        {
            PropertyName = propertyName;
            PropertyValue = propertyValue;
        }

        /// <summary>Returns the property argument.</summary>
        /// <returns>The property formatted as an argument to MSBuild.</returns>
        public override string ToString() => $"/p:{PropertyName}=\"{PropertyValue}\"";
    }
}