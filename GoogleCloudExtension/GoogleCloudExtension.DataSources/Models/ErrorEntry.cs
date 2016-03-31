// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class ErrorEntry
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}