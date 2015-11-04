// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.Projects
{
    public class DnxSdk
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class DnxGlobalJson
    {
        [JsonProperty("projects")]
        public List<string> Projects { get; set; }

        [JsonProperty("sdk")]
        public DnxSdk Sdk { get; set; }
    }
}
