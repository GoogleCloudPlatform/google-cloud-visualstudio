// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

namespace GoogleCloudExtension.OAuth
{
    /// <summary>
    /// Contains the OAUTH credentials to use for OAUTH 2.0 authentication.
    /// </summary>
    public class OAuthCredentials
    {
        public string ClientId { get; }

        public string ClientSecret { get; }

        public OAuthCredentials(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }
}