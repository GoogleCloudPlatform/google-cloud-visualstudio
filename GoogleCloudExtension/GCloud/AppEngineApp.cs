// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud
{
    public class AppEngineApp
    {
        [JsonProperty("module")]
        public string Module { get; set; }

        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("project")]
        public string Project { get; set; }

        public string IsDefaultString
        {
            get { return this.IsDefault ? "(default)" : ""; }
        }
    }
}
