// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class represents the AppEngine application data as output from the GCloud CLI
    /// and it is used to deserialize the JSON output.
    /// </summary>
    public sealed class AppEngineApplication
    {
        [JsonProperty("module")]
        public string Module { get; set; }

        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("project")]
        public string Project { get; set; }
    }
}
