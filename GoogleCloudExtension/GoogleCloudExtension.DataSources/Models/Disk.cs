// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class Disk
    {
        [JsonProperty("licenses")]
        public IList<string> Licenses { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }
    }
}