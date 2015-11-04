// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
