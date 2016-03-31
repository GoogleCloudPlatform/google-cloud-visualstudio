// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    internal class GaeServicePage
    {
        [JsonProperty("services")]
        public IEnumerable<GaeService> Items { get; internal set; }

        [JsonProperty("nextPageToken")]
        public string NextPageToken { get; internal set; }
    }
}
