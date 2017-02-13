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

using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    internal class JObjectNode : ObjectNodeTree
    {
        public JObjectNode(string name, JObject jObj, ObjectNodeTree parent) 
            : base(name, jObj, parent) { }

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
