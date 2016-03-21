// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class represents the AppEngine application data as output from the GCloud CLI
    /// and it is used to deserialize the JSON output.
    /// </summary>
    public sealed class ModuleAndVersion
    {
        [JsonProperty("module")]
        public string Module { get; set; }

        [JsonProperty("traffic_split")]
        public double TrafficSplit { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        public string Project { get; set; }

        public override string ToString()
        {
            return $"{Project}/{Module}/{Version}/{TrafficSplit}";
        }
    }
}
