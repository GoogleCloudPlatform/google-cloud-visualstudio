// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class NetworkAccessConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("natIP")]
        public string NatIP { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}