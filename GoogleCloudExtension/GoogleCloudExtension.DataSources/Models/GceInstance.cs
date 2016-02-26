// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class GceInstance
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("zone")]
        public string Zone { get; set; }

        [JsonProperty("machineType")]
        public string MachineType { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("tags")]
        public InstanceTags Tags { get; set; }

        [JsonProperty("networkInterfaces")]
        public IList<NetworkInterface> NetworkInterfaces { get; set; }

        [JsonProperty("disks")]
        public IList<Disk> Disks { get; set; }

        public string ZoneName { get; set; }

        public string ProjectId { get; set; }
    }
}