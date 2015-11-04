// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GCloud
{
    public class CoreSettings
    {
        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("project")]
        public string Project { get; set; }
    }

    public class Settings
    {
        [JsonProperty("core")]
        public CoreSettings CoreSettings { get; set; }
    }
}
