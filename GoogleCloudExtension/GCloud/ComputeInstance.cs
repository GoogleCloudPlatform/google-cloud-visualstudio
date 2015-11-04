// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GCloud
{
    public class ComputeInstance
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("cpuPlatform")]
        public string CpuPlatform { get; set; }
        [JsonProperty("machineType")]
        public string MachineType { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("zone")]
        public string Zone { get; set; }
    }
}
