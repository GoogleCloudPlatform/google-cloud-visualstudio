// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System;

namespace GoogleCloudExtension.DataSources.Models
{
    public class GaeVersion
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("servingStatus")]
        public string ServingStatus { get; set; }

        [JsonProperty("deployer")]
        public string Deployer { get; set; }

        [JsonProperty("creationTime")]
        public DateTime CreationTime { get; set; }
    }
}
