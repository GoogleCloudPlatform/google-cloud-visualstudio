// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// Defines the JSON shape of a cloud SDK component.
    /// </summary>
    public class Component
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("current_version_string")]
        public string CurrentVersion { get; set; }

        [JsonProperty("latest_version_string")]
        public string LatestVersion { get; set; }

        [JsonProperty("state")]
        public ComponentState State { get; set; }
    }
}
