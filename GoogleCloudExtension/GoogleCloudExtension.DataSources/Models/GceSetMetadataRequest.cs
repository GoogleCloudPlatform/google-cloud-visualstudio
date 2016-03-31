// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public sealed class GceSetMetadataRequest
    {
        [JsonProperty("kind")]
        public string Kind => "compute#metadata";

        [JsonProperty("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonProperty("items")]
        public IList<MetadataEntry> Items { get; set; }
    }
}