﻿// Copyright 2017 Google Inc. All Rights Reserved.
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
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Parse <seealso cref="JArray"/>.
    /// </summary>
    internal class JArrayNode : ObjectNodeTree
    {
        /// <summary>
        /// Initializes a new instance of <seealso cref="JArrayNode"/> class.
        /// </summary>
        /// <param name="name">The name to display for this <paramref name="jArrayObj"/>.</param>
        /// <param name="jArrayObj">A <seealso cref="JArray"/> object to parse.</param>
        /// <param name="parent">The parent object that owns <paramref name="jArrayObj"/>. </param>
        public JArrayNode(string name, JArray jArrayObj, ObjectNodeTree parent) 
            : base(name, jArrayObj, parent) { }

        /// <summary>
        /// Override the <seealso cref="ObjectNodeTree.ParseObjectTree(object)"/>.
        /// It is to customize the parsing procedure.
        /// </summary>
        /// <param name="obj">An <seealso cref="JArray"/> object.</param>
        protected override void ParseObjectTree(object obj)
        {
            JArray jsonArray = obj as JArray;
            List<object> objectArray = new List<object>();
            foreach (var element in jsonArray)
            {
                if (element is JArray || element is JObject)
                {
                    objectArray.Add(element);
                }
                else
                {
                    objectArray.Add(element.ToString());
                }
            }
            ParseArray(objectArray);
        }
    }
}
