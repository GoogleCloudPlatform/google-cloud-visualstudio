// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    public sealed class WindowsInstanceCredentials
    {
        [JsonProperty("username")]
        public string User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
