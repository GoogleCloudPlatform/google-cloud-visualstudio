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

using Newtonsoft.Json.Linq;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Parse <seealso cref="JObject"/> properties.
    /// </summary>
    internal class JObjectNode : ObjectNodeTree
    {
        /// <summary>
        /// Create a new instance of <seealso cref="JObjectNode"/> class.
        /// </summary>
        /// <param name="name">The name to display for this object.</param>
        /// <param name="jObj">A <seealso cref="JObjectNode"/> object to parse.</param>
        /// <param name="parent">The parent object that owns <paramref name="jObj"/>.</param>
        public JObjectNode(string name, JObject jObj, ObjectNodeTree parent) 
            : base(name, jObj, parent) { }

        /// <summary>
        /// Override the <seealso cref="ObjectNodeTree.ParseObjectTree(object)"/>.
        /// It is to customize the parsing procedure.
        /// </summary>
        /// <param name="obj">A <seealso cref="JObject"/> object.</param>
        protected override void ParseObjectTree(object obj)
        {
            JObject jsonObj = obj as JObject;
            foreach (var jItem in jsonObj)
            {
                switch (jItem.Value.GetType().Name)
                {
                    case nameof(JArray):
                    case nameof(JObject):
                        AddChildren(jItem.Key, jItem.Value);
                        break;
                    case nameof(JProperty):
                    case nameof(JValue):
                        AddChildren(jItem.Key, jItem.Value.ToString());
                        break;
                }
            }
        }
    }
}
