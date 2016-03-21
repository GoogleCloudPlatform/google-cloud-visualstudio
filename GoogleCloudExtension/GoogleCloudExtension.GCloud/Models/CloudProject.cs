// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// Information about a cloud project as returned from the GCloud CLI. Used for
    /// deserialization of the JSON output.
    /// </summary>
    public sealed class CloudProject
    {
        [JsonProperty("createTime")]
        public DateTime CreationTime { get; set; }

        [JsonProperty("lifecycleState")]
        public string State { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("projectId")]
        public string Id { get; set; }

        [JsonProperty("projectNumber")]
        public string Number { get; set; }

        public override string ToString() => Id == null ? Name : $"{Name} : {Id}";
    }
}
