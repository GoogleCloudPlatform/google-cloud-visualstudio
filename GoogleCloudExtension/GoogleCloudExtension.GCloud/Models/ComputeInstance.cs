// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class represents the data about a compute instance as returned from the 
    /// GCloud CLI, it is used to deserialize the JSON output.
    /// </summary>
    public sealed class ComputeInstance
    {
        private const string RunningState = "RUNNING";
        private const string TerminatedState = "TERMINATED";

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

        public bool IsRunning => Status == RunningState;

        public bool IsTerminated => Status == TerminatedState;
    }
}
