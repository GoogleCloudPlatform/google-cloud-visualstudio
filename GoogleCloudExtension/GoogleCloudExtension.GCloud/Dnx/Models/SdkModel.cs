// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Dnx.Models
{
    public sealed class SdkModel
    {
        /// <summary>
        /// This class is to be used to deserialize the Sdk value in the global.json
        /// object.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
