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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorer.Options
{
    internal class SerializableCloudExplorerOptions
    {
        private readonly CloudExplorerOptions _parentOptions;

        public SerializableCloudExplorerOptions(CloudExplorerOptions parentOptions)
        {
            _parentOptions = parentOptions;
        }

        /// <summary>
        /// A json string serialized version of the PubSubTopicFilters.
        /// Used for saving and loading to the settings storage.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        // ReSharper disable once UnusedMember.Global
        public string PubSubTopicFiltersJsonString
        {
            get
            {
                return _parentOptions.PubSubTopicFilters == null ?
                    null :
                    JArray.FromObject(_parentOptions.PubSubTopicFilters).ToString(Formatting.None);
            }
            set
            {
                _parentOptions.PubSubTopicFilters = value == null ?
                    null :
                    JArray.Parse(value).ToObject<List<string>>();
            }
        }
    }
}
