// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System;

namespace GCloud
{
    public class GcpProject
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

        public override string ToString()
        {
            if (Id == null)
            {
                return Name;
            }
            return $"{Name} : {Id}";
        }
    }
}
