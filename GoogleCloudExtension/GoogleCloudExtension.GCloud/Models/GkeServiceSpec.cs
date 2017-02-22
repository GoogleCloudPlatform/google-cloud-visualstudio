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

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class contains the specification of a Kubernetest service in GKE.
    /// </summary>
    public class GkeServiceSpec
    {
        // The value of the Type property for a public service.
        public const string LoadBalancerType = "LoadBalancer";

        // The value of the Type property of a cluster only service.
        public const string ClusterIpType = "ClusterIP";

        [JsonProperty("clusterIP")]
        public string ClusterIp { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}