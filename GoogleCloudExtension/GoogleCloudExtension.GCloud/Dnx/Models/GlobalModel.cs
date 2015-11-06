// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.GCloud.Dnx.Models
{
    public sealed class SdkModel
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public sealed class GlobalModel
    {
        [JsonProperty("projects")]
        public List<string> Projects { get; set; }

        [JsonProperty("sdk")]
        public SdkModel Sdk { get; set; }
    }
}
