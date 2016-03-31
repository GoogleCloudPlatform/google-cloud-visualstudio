// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System.Net;

namespace GoogleCloudExtension.DataSources
{
    internal static class WebClientExtensions
    {
        /// <summary>
        /// Initializes the client with the right header to use the given oauth token.
        /// </summary>
        /// <param name="client">The client to initialize.</param>
        /// <param name="oauthToken">The oauth token to user.</param>
        public static WebClient SetOauthToken(this WebClient client, string oauthToken)
        {
            client.Headers[HttpRequestHeader.Authorization] = $"Bearer {oauthToken}";
            return client;
        }
    }
}
