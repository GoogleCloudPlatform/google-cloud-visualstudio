// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.GCloud.Dnx.Models
{
    /// <summary>
    /// Class used for deserializing the global.json file. Only contains the properties
    /// of interest for this library.
    /// </summary>
    internal sealed class GlobalModel
    {
        [JsonProperty("projects")]
        public List<string> Projects { get; set; }
    }
}
