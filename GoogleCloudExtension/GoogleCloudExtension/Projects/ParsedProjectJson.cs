// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.Projects
{
    internal class ParsedProjectJson
    {
        [JsonProperty("dependencies")]
        public Dictionary<string, object> Dependencies { get; set; }

        [JsonProperty("frameworks")]
        public Dictionary<string, object> Frameworks { get; set; }
    }
}
