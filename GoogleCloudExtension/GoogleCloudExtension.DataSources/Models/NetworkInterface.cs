// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class NetworkInterface
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("accessConfigs")]
        public IList<NetworkAccessConfig> AccessConfigs { get; set; }

        [JsonProperty("networkIP")]
        public string NetworkIp { get; set; }
    }
}