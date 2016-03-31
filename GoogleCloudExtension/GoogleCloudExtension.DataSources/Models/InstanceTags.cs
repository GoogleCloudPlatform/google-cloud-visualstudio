// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class InstanceTags
    {
        [JsonProperty("items")]
        public IList<string> Items { get; set; }
    }
}