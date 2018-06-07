﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// Defines the JSON shape of a cloud SDK component.
    /// </summary>
    public sealed class CloudSdkComponent
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("current_version_string")]
        public string CurrentVersion { get; set; }

        [JsonProperty("latest_version_string")]
        public string LatestVersion { get; set; }

        [JsonProperty("state")]
        public CloudSdkComponentState State { get; set; }
    }
}
