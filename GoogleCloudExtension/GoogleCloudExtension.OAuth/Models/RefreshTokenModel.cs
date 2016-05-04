// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.OAuth.Models
{
    /// <summary>
    /// Model for the OAUTH response when exchanging an access code for a refresh token.
    /// </summary>
    internal class RefreshTokenModel
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
