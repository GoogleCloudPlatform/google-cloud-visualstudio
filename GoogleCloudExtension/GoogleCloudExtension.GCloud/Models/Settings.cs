// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class is used to deserialize the core settings of GCloud from its JSON
    /// representation.
    /// </summary>
    internal sealed class CoreSettings
    {
        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("project")]
        public string Project { get; set; }
    }

    /// <summary>
    /// This class is used to deserialize GCloud settings from its JSON representation.
    /// </summary>
    internal sealed class Settings
    {
        [JsonProperty("core")]
        public CoreSettings CoreSettings { get; set; }
    }
}
