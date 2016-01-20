// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class VersioningValue
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }

    internal class Bucket
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("timeCreated")]
        public DateTime Created { get; set; }

        [JsonProperty("updated")]
        public DateTime Updated { get; set; }

        [JsonProperty("selfLink")]
        public string SelfLink { get; set; }

        [JsonProperty("versioning")]
        public VersioningValue Versioning { get; set; }

        [JsonProperty("storageClass")]
        public string StorageClass { get; set; }
    }
}